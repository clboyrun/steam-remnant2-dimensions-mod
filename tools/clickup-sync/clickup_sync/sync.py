"""Pull (ClickUp -> files), push (files -> ClickUp), and the quiet-period gate."""
import json
import shutil
import time
from pathlib import Path
from urllib.parse import quote

from .api import ApiError
from . import cache as C

README = """# ClickUp cache (written by ClickUp Sync)

One markdown file per ticket in `<List name>/<id> - <slug>.md`. Edit the front matter and the
sections; the tray tool pushes the changes after the folder has been quiet (no file created,
modified or deleted) for the configured time (default 3 minutes).

* Editable keys: name, status, assignees (usernames), priority (urgent/high/normal/low/null),
  start_date / due_date (YYYY-MM-DD), time_estimate_hours, plus the `## Description` section.
  Read-only keys (id, url, list, parent, depends_on, blocks, tags, remote_updated) are ignored.
* To close a ticket set `status: "complete"` (and assign the person who should own it).
  start_date is NOT pushed unless "Push start dates" is switched on in the tool.
* New ticket: create `<List name>/NEW - <anything>.md` (or `py -m clickup_sync --new "<List>" "<Name>"`).
  Same keys, `status` may be empty. It becomes a ClickUp task on the next push and the file is
  renamed to `<id> - <slug>.md`. Parents are created first.
* Hierarchy and links (existing and new tickets): `parent` (id or exact ticket name; changing it
  moves the subtask), `depends_on` (this waits on these), `blocks` (these wait on this), `links`
  (plain "related" links). Each value is a list of ticket ids or exact ticket names, including
  names of other NEW files in the same push.
* `tags`: list of tag names (lower-case); added/removed on push, tags missing from the space are created.
  Milestones: tickets carry an `ms-…` tag, the milestone card (Milestones list) `depends_on` its tickets;
  `_milestones.md` at the cache root is a generated overview grouped by those tags.
* Closing a ticket with nobody assigned assigns the configured closer (default Astra).
* `## Post comment`: text here is posted as a comment on the next push, then cleared.
* `## Agent notes`: local only, never pushed, kept across pulls. Read it before working on a
  ticket and add research / decisions / evidence there, so nothing has to be re-fetched.
* `_index.md` in each list folder: one-line overview of every ticket (read this first).
* `.state/` holds the last-synced remote snapshot used to detect your edits. Don't touch it.
* Deleting a file does nothing remotely; the next pull recreates it.
"""


class QuietGate:
    """Fires once the folder has had no create/modify/delete for `quiet` seconds after a change."""

    def __init__(self, root, quiet=180, clock=time.time):
        self.root, self.quiet, self.clock = Path(root), quiet, clock
        self.snap = self._snapshot()
        self.last_change = None  # None = nothing pending

    def _snapshot(self):
        out = {}
        if self.root.is_dir():
            for p in self.root.rglob("*"):
                if C.STATE_DIR in p.relative_to(self.root).parts or not p.is_file():
                    continue
                try:
                    st = p.stat()
                    out[str(p)] = (st.st_mtime_ns, st.st_size)
                except OSError:
                    pass
        return out

    def rebase(self):
        """Accept the current state (our own pull/push writes) without arming the gate."""
        self.snap, self.last_change = self._snapshot(), None

    def poll(self):
        """True when edits are pending and the folder has been still long enough."""
        now = self._snapshot()
        if now != self.snap:
            self.snap, self.last_change = now, self.clock()
            return False
        return self.last_change is not None and self.clock() - self.last_change >= self.quiet

    def seconds_left(self):
        return None if self.last_change is None else max(0, self.quiet - (self.clock() - self.last_change))


class Engine:
    def __init__(self, cfg, client, log=print):
        self.cfg, self.client, self.log = cfg, client, log
        self.cache = C.Cache(cfg.cache_path())
        self.members = {}

    # ---------- discovery ----------
    def _team(self):
        teams = self.client.get("/team").get("teams", [])
        if not teams:
            raise RuntimeError("API key sees no workspace")
        t = next((t for t in teams if t["id"] == self.cfg.team_id), teams[0])
        self.members = {}
        for m in t.get("members", []):
            u = m.get("user", {})
            if u.get("id"):
                self.members[(u.get("username") or u.get("email") or str(u["id"]))] = u["id"]
        return t["id"]

    def _lists(self, team):
        spaces = self.client.get(f"/team/{team}/space", {"archived": "false"}).get("spaces", [])
        wanted = self.cfg.space_list()
        for s in spaces:
            if wanted and s["id"] not in wanted:
                continue
            for l in self.client.get(f"/space/{s['id']}/list", {"archived": "false"}).get("lists", []):
                l["_space_id"] = s["id"]
                yield s["name"], l
            for f in self.client.get(f"/space/{s['id']}/folder", {"archived": "false"}).get("folders", []):
                for l in f.get("lists", []) or self.client.get(f"/folder/{f['id']}/list").get("lists", []):
                    l["_space_id"] = s["id"]
                    yield s["name"], l

    def _tasks(self, list_id):
        page = 0
        while True:
            r = self.client.get(f"/list/{list_id}/task", {
                "page": page, "subtasks": "true", "include_closed": str(self.cfg.include_closed).lower(),
                "include_markdown_description": "true"})
            tasks = r.get("tasks", [])
            yield from tasks
            if r.get("last_page", len(tasks) < 100) or not tasks:
                return
            page += 1

    # ---------- pull ----------
    def pull(self):
        root = self.cache.root
        root.mkdir(parents=True, exist_ok=True)
        (root / "README.md").write_text(README, encoding="utf-8", newline="\n")
        team = self._team()
        existing, stats = self.cache.files(), {"new": 0, "updated": 0, "kept_local": 0, "same": 0}
        notes_backup = self._load_notes_backup()
        lists, all_rows = {}, []
        for space_name, lst in self._lists(team):
            lists[C.safe_dir(lst["name"])] = {"id": lst["id"], "name": lst["name"], "space_id": lst.get("_space_id", "")}
            rows = []
            for task in self._tasks(lst["id"]):
                f = C.normalize(task, lst["name"], space_name)
                rows.append(f)
                path = existing.get(f["id"])
                if path is None:
                    self.cache.write(f, notes=notes_backup.get(f["id"], ""))
                    stats["new"] += 1
                    continue
                try:
                    changes, comment = self.cache.dirty(f["id"], path)
                    _, _, notes = self.cache.read(path)
                except ValueError:
                    self.log(f"unreadable file kept as is: {path}")
                    stats["kept_local"] += 1
                    continue
                base = self.cache.load_base(f["id"])
                if changes or comment:
                    if base and base.get("remote_updated") != f["remote_updated"]:
                        self.log(f"{f['id']}: remote changed AND local edits pending; local file kept, "
                                 f"push will send only your edited keys")
                    stats["kept_local"] += 1
                    continue
                if base and base.get("remote_updated") == f["remote_updated"] and path == self.cache.path_for(f):
                    stats["same"] += 1
                    continue
                notes = notes if not notes.startswith("(local only") else ""
                self.cache.write(f, notes=notes, old_path=path)
                stats["updated"] += 1
            self.cache.write_index(lst["name"], rows)
            all_rows += rows
        self.cache.write_milestones(all_rows, self.cfg.milestone_tag_prefix)
        self.cache.save_lists({**self.cache.load_lists(), **lists})
        self.log(f"pull done: {stats} ({self.client.calls} API calls so far)")
        return stats

    def _load_notes_backup(self):
        p = self.cache.root / ".notes_backup.json"
        try:
            return json.loads(p.read_text(encoding="utf-8"))
        except (OSError, ValueError):
            return {}

    # ---------- push ----------
    def pending(self):
        out = []  # existing tickets with edits
        for tid, path in self.cache.files().items():
            try:
                changes, comment = self.cache.dirty(tid, path)
            except ValueError as e:
                self.log(f"skip {path.name}: {e}")
                continue
            if changes or comment:
                out.append((tid, path, changes, comment))
        return out

    def _payload(self, changes):
        body, warn = {}, []
        for k, v in changes.items():
            if k == "name":
                body["name"] = v
            elif k == "description":
                body["markdown_description"] = v
            elif k == "status":
                if v:
                    body["status"] = v
            elif k == "priority":
                if v is None:
                    body["priority"] = None
                elif v in C.PRIORITIES:
                    body["priority"] = C.PRIORITIES[v]
                else:
                    warn.append(f"unknown priority {v!r}")
            elif k == "due_date":
                body["due_date"] = C.date_to_ms(v) if v else None
                body["due_date_time"] = False
            elif k == "start_date":
                if self.cfg.push_start_date:
                    body["start_date"] = C.date_to_ms(v) if v else None
                    body["start_date_time"] = False
                else:
                    warn.append("start_date change not pushed (disabled)")
            elif k == "parent":
                pid = self._resolve(v) if v else None
                if v and not pid:
                    warn.append(f"unknown parent {v!r}")
                else:
                    body["parent"] = pid
            elif k == "time_estimate_hours":
                body["time_estimate"] = int(v * 3600000) if v is not None else None
        return body, warn

    def _assignee_delta(self, tid, wanted):
        base = (self.cache.load_base(tid) or {}).get("assignees", [])
        add, rem, warn = [], [], []
        for name in set(wanted) - set(base):
            if name in self.members:
                add.append(self.members[name])
            elif name.isdigit():
                add.append(int(name))
            else:
                warn.append(f"unknown assignee {name!r}")
        for name in set(base) - set(wanted):
            if name in self.members:
                rem.append(self.members[name])
        return add, rem, warn

    # ---------- references, links, creation ----------
    def _resolve(self, ref):
        """Ticket id, or the exact name (case-insensitive) of a known/just-created ticket -> id."""
        ref = str(ref).strip()
        if ref in self._ids:
            return ref
        hit = self._names.get(ref.lower())
        if hit:
            return hit
        if " " not in ref and ref.lower() != "new" and ref.isalnum():
            return ref  # looks like an id we haven't cached (other space); let the API decide
        self.log(f"cannot resolve reference {ref!r}")
        return None

    def _index_refs(self):
        self._ids = set(self.cache.files())
        self._names = {}
        dup = set()
        for tid in self._ids:
            b = self.cache.load_base(tid)
            if b and b.get("name"):
                k = b["name"].strip().lower()
                if k in self._names:
                    dup.add(k)
                self._names[k] = tid
        for k in dup:
            del self._names[k]

    def _apply_links(self, tid, old, new):
        """Diff depends_on / blocks / links lists; one API call per change."""
        for key in C.LIST_KEYS:
            want = {self._resolve(x) for x in new.get(key, [])} - {None}
            have = set(old.get(key, []))
            for other in sorted(want - have):
                try:
                    if key == "links":
                        self.client.post(f"/task/{tid}/link/{other}", {})
                    else:
                        self.client.post(f"/task/{tid}/dependency",
                                         {"depends_on": other} if key == "depends_on" else {"dependency_of": other})
                except Exception as e:
                    self.log(f"{tid}: could not add {key} {other}: {e}")
            for other in sorted(have - want):
                try:
                    if key == "links":
                        self.client.delete(f"/task/{tid}/link/{other}")
                    else:
                        self.client.delete(f"/task/{tid}/dependency",
                                           {"depends_on": other} if key == "depends_on" else {"dependency_of": other})
                except Exception as e:
                    self.log(f"{tid}: could not remove {key} {other}: {e}")

    def _space_of(self, list_id):
        return next((l.get("space_id") for l in self.cache.load_lists().values() if l["id"] == list_id), "")

    def _apply_tags(self, tid, old, new, space_id=""):
        for tag in sorted(set(new) - set(old)):
            try:
                try:
                    self.client.post(f"/task/{tid}/tag/{quote(tag, safe='')}", {})
                except ApiError as e:
                    if e.status in (400, 404) and space_id:  # tag not in the space yet
                        self.client.post(f"/space/{space_id}/tag", {"tag": {"name": tag, "tag_fg": "#ffffff", "tag_bg": "#7b68ee"}})
                        self.client.post(f"/task/{tid}/tag/{quote(tag, safe='')}", {})
                    else:
                        raise
            except Exception as e:
                self.log(f"{tid}: could not add tag {tag}: {e}")
        for tag in sorted(set(old) - set(new)):
            try:
                self.client.delete(f"/task/{tid}/tag/{quote(tag, safe='')}")
            except Exception as e:
                self.log(f"{tid}: could not remove tag {tag}: {e}")

    def create_new(self, dry_run=False):
        """Create ClickUp tasks from `NEW - *.md` files. Parents first; links after all exist."""
        files = self.cache.new_files()
        if not files:
            return 0
        if not self.members:
            self._team()
        lists = self.cache.load_lists()
        queue = []
        for p in files:
            try:
                fields, comment, notes = self.cache.read(p)
            except ValueError as e:
                self.log(f"skip {p.name}: {e}")
                continue
            lst = lists.get(p.parent.name) or next(
                (l for l in lists.values() if l["name"] == p.parent.name), None)
            if not lst:
                self.log(f"{p}: folder is not a known list (run a pull first); skipped")
                continue
            queue.append((p, C.clean(fields), comment, notes, lst))
        made, pending_links = 0, []
        while queue:
            qnames = {it[1]["name"].strip().lower() for it in queue}
            progress, rest = False, []
            for item in queue:
                p, f, comment, notes, lst = item
                parent, pref = None, f["parent"]
                if pref:
                    k = pref.strip().lower()
                    if k in qnames and k not in self._names:
                        rest.append(item)  # parent is itself a NEW file not created yet
                        continue
                    parent = self._resolve(pref)
                    if not parent:
                        rest.append(item)
                        continue
                if not f["name"]:
                    self.log(f"{p.name}: no name; skipped")
                    continue
                body = {"name": f["name"], "markdown_description": f["description"]}
                if f["status"]:
                    body["status"] = f["status"]
                if parent:
                    body["parent"] = parent
                if f["priority"] in C.PRIORITIES:
                    body["priority"] = C.PRIORITIES[f["priority"]]
                if f["due_date"]:
                    body["due_date"], body["due_date_time"] = C.date_to_ms(f["due_date"]), False
                if f["start_date"] and self.cfg.push_start_date:
                    body["start_date"], body["start_date_time"] = C.date_to_ms(f["start_date"]), False
                if f["time_estimate_hours"] is not None:
                    body["time_estimate"] = int(f["time_estimate_hours"] * 3600000)
                ids = [self.members[a] for a in f["assignees"] if a in self.members]
                for a in f["assignees"]:
                    if a not in self.members:
                        self.log(f"{p.name}: unknown assignee {a!r}")
                if ids:
                    body["assignees"] = ids
                self.log(f"create '{f['name']}' in {lst['name']}" + (f" under {parent}" if parent else ""))
                if dry_run:
                    progress = True
                    continue
                try:
                    task = self.client.post(f"/list/{lst['id']}/task", body)
                except Exception as e:
                    self.log(f"{p.name}: create failed, will retry: {e}")
                    continue
                tid = task["id"]
                self._ids.add(tid)
                self._names[f["name"].strip().lower()] = tid
                nf = C.normalize(task, lst["name"])
                nf["list"] = lst["name"]
                self.cache.write(nf, notes=notes if not notes.startswith("(local only") else "", old_path=p)
                if comment:
                    try:
                        self.client.post(f"/task/{tid}/comment", {"comment_text": comment, "notify_all": False})
                    except Exception as e:
                        self.log(f"{tid}: comment failed: {e}")
                pending_links.append((tid, {k: f[k] for k in C.LIST_KEYS}, f["tags"], lst.get("space_id", "")))
                made += 1
                progress = True
            queue = rest
            if not progress:
                break
        for p, f, *_ in queue:
            self.log(f"{p.name}: parent {f['parent']!r} not found/created; left for the next push")
        for tid, wanted, tags, space_id in pending_links:
            self._apply_links(tid, {}, wanted)
            self._apply_tags(tid, [], tags, space_id)
            try:
                fresh = self.client.get(f"/task/{tid}", {"include_markdown_description": "true"})
                path = self.cache.files().get(tid)
                b = self.cache.load_base(tid) or {}
                if path:
                    _, _, n = self.cache.read(path)
                    nf = C.normalize(fresh, b.get("list", ""), b.get("space", ""))
                    nf["list"] = (fresh.get("list") or {}).get("name") or nf["list"]
                    self.cache.write(nf, notes=n if not n.startswith("(local only") else "", old_path=path)
            except Exception as e:
                self.log(f"{tid}: refresh failed (next pull fixes it): {e}")
        self.log(f"create done: {made} new tickets")
        return made

    def push(self, dry_run=False):
        self._closed = {x.strip().lower() for x in self.cfg.closed_statuses.split(",") if x.strip()}
        self._index_refs()
        created = self.create_new(dry_run)
        todo = self.pending()
        if not todo:
            self.log("push: nothing to send")
            return created
        if not self.members:
            self._team()
        sent = 0
        for tid, path, changes, comment in todo:
            link_new = {k: changes[k] for k in C.LIST_KEYS if k in changes}
            body, warns = self._payload({k: v for k, v in changes.items()
                                         if k not in ("assignees", "tags") and k not in C.LIST_KEYS})
            if changes.get("status", "").lower() in self._closed and self.cfg.close_assignee:
                cur = changes.get("assignees", (self.cache.load_base(tid) or {}).get("assignees", []))
                who = next((u for u in self.members if u.lower() == self.cfg.close_assignee.lower()), None)
                if not cur and who:
                    changes = {**changes, "assignees": [who]}
                    self.log(f"{tid}: closing with no assignee -> assigning {who}")
                elif not cur:
                    warns.append(f"closing with no assignee and {self.cfg.close_assignee!r} not found")
            if "assignees" in changes:
                add, rem, w = self._assignee_delta(tid, changes["assignees"])
                warns += w
                if add or rem:
                    body["assignees"] = {"add": add, "rem": rem}
            for w in warns:
                self.log(f"{tid}: {w}")
            self.log(f"{tid}: push {sorted(body)}{' + comment' if comment else ''}")
            if dry_run:
                continue
            try:
                if body:
                    self.client.put(f"/task/{tid}", body)
                if comment:
                    self.client.post(f"/task/{tid}/comment", {"comment_text": comment, "notify_all": False})
                if "tags" in changes:
                    cur = self.cache.load_base(tid) or {}
                    self._apply_tags(tid, cur.get("tags", []), changes["tags"], self._space_of(cur.get("list_id", "")))
                if link_new:
                    cur = self.cache.load_base(tid) or {}
                    self._apply_links(tid, {k: cur.get(k, []) for k in C.LIST_KEYS}, link_new)
                fresh = self.client.get(f"/task/{tid}", {"include_markdown_description": "true"})
            except Exception as e:  # keep local edits; next round retries
                self.log(f"{tid}: push failed, will retry: {e}")
                continue
            base = self.cache.load_base(tid) or {}
            f = C.normalize(fresh, base.get("list") or fresh.get("list", {}).get("name", ""), base.get("space", ""))
            f["list"] = (fresh.get("list") or {}).get("name") or f["list"]
            kept = {}
            if "start_date" in changes and not self.cfg.push_start_date:
                kept["start_date"] = changes["start_date"]  # keep user's local value, still "dirty"
            _, _, notes = self.cache.read(path)
            if kept:
                f.update(kept)
                self.cache.write(f, notes=notes, old_path=path)
                b = self.cache.load_base(tid)
                b["start_date"] = C.normalize(fresh, "")["start_date"]
                (self.cache.state / f"{tid}.json").write_text(json.dumps(b), encoding="utf-8")
            else:
                self.cache.write(f, notes=notes, old_path=path)
            sent += 1
        self.log(f"push done: {sent} edited, {created} created")
        return sent + created

    # ---------- reset ----------
    def reset(self, keep_notes=True):
        """Delete the cache contents. Agent notes are stashed and restored on the next pull."""
        root = self.cache.root
        notes = {}
        if keep_notes:
            for tid, p in self.cache.files().items():
                try:
                    n = self.cache.read(p)[2]
                    if n and not n.startswith("(local only"):
                        notes[tid] = n
                except (ValueError, OSError):
                    pass
            notes.update(self._load_notes_backup())
        if root.is_dir():
            for child in root.iterdir():
                shutil.rmtree(child) if child.is_dir() else child.unlink()
        root.mkdir(parents=True, exist_ok=True)
        if notes:
            (root / ".notes_backup.json").write_text(json.dumps(notes), encoding="utf-8")
        self.log(f"cache reset ({len(notes)} agent notes stashed)" if keep_notes else "cache reset (notes discarded)")
