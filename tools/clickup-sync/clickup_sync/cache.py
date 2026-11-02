"""Ticket <-> markdown file format.

<cache>/<List name>/<task id> - <slug>.md
    ---
    key: json-value          (editable keys are pushed; read-only keys are ignored)
    ---
    ## Description
    ## Post comment          (text is posted as a comment, then cleared)
    ## Agent notes           (local only, never pushed; kept across pulls)
"""
import json
import re
from datetime import datetime, timezone
from pathlib import Path

EDITABLE = ["name", "status", "assignees", "priority", "start_date", "due_date",
            "time_estimate_hours", "parent", "depends_on", "blocks", "links", "description"]
LIST_KEYS = ("depends_on", "blocks", "links")
READONLY = ["id", "url", "list", "list_id", "space", "tags", "remote_updated"]
PRIORITIES = {"urgent": 1, "high": 2, "normal": 3, "low": 4}
PRIORITY_NAMES = {v: k for k, v in PRIORITIES.items()}
SECTION_RE = re.compile(r"^## (Description|Post comment|Agent notes)[ \t]*$", re.M)
FILE_RE = re.compile(r"^([0-9a-z]+) - .*\.md$")
NEW_RE = re.compile(r"^NEW\b.*\.md$", re.I)
STATE_DIR = ".state"


def slug(text, n=60):
    s = re.sub(r"[^A-Za-z0-9]+", "-", text).strip("-").lower()
    return (s[:n].strip("-")) or "ticket"


def safe_dir(name):
    return re.sub(r'[<>:"/\\|?*\x00-\x1f]', "_", name).strip(" .") or "list"


def ms_to_date(ms):
    if not ms:
        return None
    return datetime.fromtimestamp(int(ms) / 1000, timezone.utc).strftime("%Y-%m-%d")


def date_to_ms(s):
    """Noon UTC so the date shows the same day in any timezone."""
    d = datetime.strptime(s, "%Y-%m-%d").replace(hour=12, tzinfo=timezone.utc)
    return int(d.timestamp() * 1000)


def normalize(task, list_name, space_name=""):
    """ClickUp task JSON -> flat dict of the fields we mirror."""
    pr = (task.get("priority") or {}).get("priority")
    est = task.get("time_estimate")
    deps = task.get("dependencies") or []
    me = task["id"]
    links = set()
    for l in task.get("linked_tasks") or []:
        other = l.get("link_id") if l.get("task_id") == me else l.get("task_id")
        if other and other != me:
            links.add(other)
    return {
        "id": me, "url": task.get("url", f"https://app.clickup.com/t/{me}"),
        "list": list_name, "list_id": (task.get("list") or {}).get("id", ""), "space": space_name,
        "parent": task.get("parent") or None, "links": sorted(links),
        "depends_on": sorted(d["depends_on"] for d in deps if d.get("task_id") == me and d.get("depends_on")),
        "blocks": sorted(d["task_id"] for d in deps if d.get("depends_on") == me),
        "tags": [t["name"] for t in task.get("tags", [])],
        "remote_updated": str(task.get("date_updated") or ""),
        "name": task.get("name", ""),
        "status": ((task.get("status") or {}).get("status") or "").lower(),
        "assignees": sorted(a.get("username") or a.get("email") or str(a["id"]) for a in task.get("assignees", [])),
        "priority": pr.lower() if pr else None,
        "start_date": ms_to_date(task.get("start_date")),
        "due_date": ms_to_date(task.get("due_date")),
        "time_estimate_hours": round(int(est) / 3600000, 2) if est else None,
        "description": task.get("markdown_description") or task.get("description") or "",
    }


def render(fields, comment="", notes=""):
    out = ["---"]
    for k in READONLY + EDITABLE:
        if k == "description" or k not in fields:
            continue
        out.append(f"{k}: {json.dumps(fields[k], ensure_ascii=False)}")
    out += ["---", "", "## Description", fields.get("description", "").rstrip(), "",
            "## Post comment", comment.strip(), "", "## Agent notes", notes.strip() or
            "(local only; never pushed. Record research, decisions, evidence, scope changes here.)", ""]
    return "\n".join(out)


def parse(text):
    """-> (fields dict incl. description, comment, notes)."""
    text = text.replace("\r\n", "\n")
    m = re.match(r"^---\n(.*?)\n---\n", text, re.S)
    if not m:
        raise ValueError("missing front matter")
    fields = {}
    for line in m.group(1).split("\n"):
        if ":" not in line or line.lstrip().startswith("#"):
            continue
        k, v = line.split(":", 1)
        v = v.strip()
        try:
            fields[k.strip()] = json.loads(v)
        except ValueError:
            fields[k.strip()] = v or None  # hand-typed bare value
    body = text[m.end():]
    parts = SECTION_RE.split(body)
    sections = {parts[i]: parts[i + 1].strip() for i in range(1, len(parts) - 1, 2)}
    fields["description"] = sections.get("Description", "")
    return fields, sections.get("Post comment", ""), sections.get("Agent notes", "")


def clean(fields):
    """Coerce hand-edited values into canonical form so diffs are stable."""
    f = {k: fields.get(k) for k in EDITABLE}
    f["status"] = (f["status"] or "").strip().lower()
    a = f["assignees"]
    f["assignees"] = sorted(x.strip() for x in ([a] if isinstance(a, str) else a or []) if x and x.strip())
    p = f["priority"]
    f["priority"] = str(p).lower() if p not in (None, "", "none") else None
    for k in ("start_date", "due_date"):
        f[k] = (str(f[k])[:10] if f[k] else None)
    h = f["time_estimate_hours"]
    f["time_estimate_hours"] = round(float(h), 2) if h not in (None, "") else None
    f["description"] = (f["description"] or "").strip()
    f["parent"] = (str(f["parent"]).strip() or None) if f["parent"] else None
    for k in LIST_KEYS:
        v = f[k]
        f[k] = sorted({str(x).strip() for x in ([v] if isinstance(v, str) else v or []) if str(x).strip()})
    return f


class Cache:
    def __init__(self, root):
        self.root = Path(root)
        self.state = self.root / STATE_DIR

    # ---- lookup ----
    def files(self):
        out = {}
        if self.root.is_dir():
            for p in self.root.glob("*/*.md"):
                m = FILE_RE.match(p.name)
                if m and p.parent.name != STATE_DIR and m.group(1).lower() != "new":
                    out[m.group(1)] = p
        return out

    def new_files(self):
        """Files named `NEW - <slug>.md` in a list folder: tickets to create."""
        out = []
        if self.root.is_dir():
            for p in sorted(self.root.glob("*/*.md")):
                if NEW_RE.match(p.name) and not (FILE_RE.match(p.name) and p.name.split(" ")[0] != "NEW"):
                    out.append(p)
        return out

    def save_lists(self, lists):
        self.state.mkdir(parents=True, exist_ok=True)
        (self.state / "lists.json").write_text(json.dumps(lists), encoding="utf-8")

    def load_lists(self):
        try:
            return json.loads((self.state / "lists.json").read_text(encoding="utf-8"))
        except (OSError, ValueError):
            return {}

    def write_new(self, list_name, name):
        p = self.root / safe_dir(list_name) / f"NEW - {slug(name)}.md"
        p.parent.mkdir(parents=True, exist_ok=True)
        f = {"name": name, "status": "", "assignees": [], "priority": None, "start_date": None, "due_date": None,
             "time_estimate_hours": None, "parent": None, "depends_on": [], "blocks": [], "links": [],
             "description": ""}
        p.write_text(render(f), encoding="utf-8", newline="\n")
        return p

    def path_for(self, fields):
        return self.root / safe_dir(fields["list"]) / f"{fields['id']} - {slug(fields['name'])}.md"

    # ---- base snapshot (remote state at last sync) ----
    def load_base(self, tid):
        try:
            return json.loads((self.state / f"{tid}.json").read_text(encoding="utf-8"))
        except (OSError, ValueError):
            return None

    def save_base(self, fields):
        self.state.mkdir(parents=True, exist_ok=True)
        (self.state / f"{fields['id']}.json").write_text(
            json.dumps({k: fields.get(k) for k in READONLY + EDITABLE}, ensure_ascii=False), encoding="utf-8")

    # ---- read/write ----
    def read(self, path):
        text = Path(path).read_text(encoding="utf-8")
        return parse(text)

    def write(self, fields, comment="", notes="", old_path=None):
        p = self.path_for(fields)
        p.parent.mkdir(parents=True, exist_ok=True)
        p.write_text(render(fields, comment, notes), encoding="utf-8", newline="\n")
        if old_path and Path(old_path) != p and Path(old_path).exists():
            Path(old_path).unlink()
        self.save_base(fields)
        return p

    def dirty(self, tid, path):
        """-> (changed editable keys, comment text) vs. the last-synced remote state."""
        base = self.load_base(tid)
        fields, comment, _ = self.read(path)
        if base is None:
            return {}, comment
        mine, theirs = clean(fields), clean(base)
        # keys missing from an older snapshot are not treated as edits
        return {k: mine[k] for k in EDITABLE if k in base and mine[k] != theirs[k]}, comment

    def write_index(self, list_name, rows):
        d = self.root / safe_dir(list_name)
        lines = [f"# {list_name}", "", "Generated index. Do not edit (overwritten on every pull).", "",
                 "| id | status | name | est h | parent |", "|---|---|---|---|---|"]
        for r in sorted(rows, key=lambda r: r["name"].lower()):
            lines.append(f"| {r['id']} | {r['status']} | {r['name'].replace('|', '/')} | "
                         f"{r['time_estimate_hours'] or ''} | {r['parent'] or ''} |")
        (d / "_index.md").write_text("\n".join(lines) + "\n", encoding="utf-8", newline="\n")
