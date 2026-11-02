"""Pull (ClickUp -> files), push (files -> ClickUp), and the quiet-period gate."""
import json
import shutil
import time
from pathlib import Path

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
                yield s["name"], l
            for f in self.client.get(f"/space/{s['id']}/folder", {"archived": "false"}).get("folders", []):
                for l in f.get("lists", []) or self.client.get(f"/folder/{f['id']}/list").get("lists", []):
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
        for space_name, lst in self._lists(team):
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
        out = []
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

    def push(self, dry_run=False):
        todo = self.pending()
        if not todo:
            self.log("push: nothing to send")
            return 0
        if not self.members:
            self._team()
        sent = 0
        for tid, path, changes, comment in todo:
            body, warns = self._payload({k: v for k, v in changes.items() if k != "assignees"})
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
        self.log(f"push done: {sent} tickets")
        return sent

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
