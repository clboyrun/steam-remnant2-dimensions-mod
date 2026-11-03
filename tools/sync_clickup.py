"""Write a status snapshot of the Remnant 2 Dimensions ClickUp folder as JSON.

The GitHub Pages status page reads this file. It runs in a GitHub Action with the
ClickUp token in a repository secret, so the token never reaches the browser.

    CLICKUP_API_TOKEN=pk_... python tools/sync_clickup.py --out status.json

Only task names, statuses, hierarchy, estimates, dependencies and links are written.
Descriptions, comments and assignees are left out because the page is public.
"""

from __future__ import annotations

import argparse
import json
import os
import sys
import time
import urllib.error
import urllib.request
from datetime import datetime, timezone
from pathlib import Path

from burndown import build_burndown

API = "https://api.clickup.com/api/v2"
DEFAULT_FOLDER_ID = "901414897445"  # "Remnant 2 Dimensions"; every list in its space is read
HERE = Path(__file__).resolve().parent


class ClickUp:
    def __init__(self, token: str):
        self.token = token

    def get(self, path: str, params: dict | None = None) -> dict:
        query = ""
        if params:
            query = "?" + "&".join(f"{k}={v}" for k, v in params.items())
        req = urllib.request.Request(API + path + query, headers={"Authorization": self.token})
        for attempt in range(4):
            try:
                with urllib.request.urlopen(req, timeout=30) as resp:
                    return json.load(resp)
            except urllib.error.HTTPError as e:
                if e.code == 429 or e.code >= 500:
                    time.sleep(2 ** (attempt + 1))
                    continue
                body = e.read().decode("utf-8", "replace")[:300]
                raise SystemExit(f"ClickUp {path} returned {e.code}: {body}")
        raise SystemExit(f"ClickUp {path} kept failing after retries")

    def folder(self, folder_id: str) -> dict:
        return self.get(f"/folder/{folder_id}")

    def lists(self, folder_id: str) -> list[dict]:
        # GET /folder/{id} doesn't always include its lists, so ask for them directly.
        return self.get(f"/folder/{folder_id}/list", {"archived": "false"}).get("lists", [])

    def space_lists(self, space_id: str) -> list[dict]:
        out = self.get(f"/space/{space_id}/list", {"archived": "false"}).get("lists", [])
        out.sort(key=lambda l: int(l.get("orderindex") or 0))
        for f in self.get(f"/space/{space_id}/folder", {"archived": "false"}).get("folders", []):
            out.extend(f.get("lists") or self.lists(f["id"]))
        for i, l in enumerate(out):  # keep folders' own order, folderless lists first
            l["orderindex"] = i
        return out

    def tasks(self, list_id: str) -> list[dict]:
        out, page = [], 0
        while True:
            data = self.get(
                f"/list/{list_id}/task",
                {"include_closed": "true", "subtasks": "true", "archived": "false", "page": page},
            )
            out.extend(data.get("tasks", []))
            if data.get("last_page", True) or not data.get("tasks"):
                return out
            page += 1


def is_done(status: dict) -> bool:
    return (status or {}).get("type") in ("closed", "done")


def task_record(t: dict, tracker_ids: dict[str, str]) -> dict:
    status = t.get("status") or {}
    estimate = t.get("time_estimate")
    # For the dependency wheel: tasks this one waits on, and "related" links (not blockers).
    waits_on = sorted({d["depends_on"] for d in t.get("dependencies") or []
                       if d.get("task_id") == t["id"] and d.get("depends_on")})
    links = sorted({(l.get("link_id") if l.get("task_id") == t["id"] else l.get("task_id"))
                    for l in t.get("linked_tasks") or []} - {None, t["id"]})
    return {
        "id": t["id"],
        "tracker_id": tracker_ids.get(t["id"]),
        "name": t.get("name", ""),
        "status": status.get("status", "unknown"),
        "status_color": status.get("color"),
        "done": is_done(status),
        "open": status.get("type") == "open",
        "parent": t.get("parent"),
        "estimate_hours": round(int(estimate) / 3_600_000, 2) if estimate else None,
        "waits_on": waits_on,
        "links": links,
        "url": t.get("url") or f"https://app.clickup.com/t/{t['id']}",
        "updated": int(t["date_updated"]) if t.get("date_updated") else None,
        "order": float(t.get("orderindex") or 0),
    }


def build_snapshot(
    folder: dict, tasks_by_list: dict[str, list[dict]], tracker_ids: dict[str, str], known_statuses: list[dict] = ()
) -> dict:
    # Every status ClickUp defines is listed, in ClickUp's order, even with no tasks in it yet.
    lists, statuses = [], {}
    for k in sorted(known_statuses, key=lambda k: int(k.get("orderindex") or 0)):
        statuses.setdefault(k["status"], {"status": k["status"], "color": k.get("color"), "done": is_done(k), "open": k.get("type") == "open", "count": 0})
    for lst in sorted(folder.get("lists", []), key=lambda l: int(l.get("orderindex") or 0)):
        tasks = [task_record(t, tracker_ids) for t in tasks_by_list.get(lst["id"], [])]
        tasks.sort(key=lambda r: r["order"])
        for r in tasks:
            s = statuses.setdefault(r["status"], {"status": r["status"], "color": r["status_color"], "done": r["done"], "open": r["open"], "count": 0})
            s["color"] = s["color"] or r["status_color"]
            s["count"] += 1
        lists.append({"id": lst["id"], "name": lst["name"], "tasks": tasks})

    snapshot = {
        "generated_at": datetime.now(timezone.utc).isoformat(timespec="seconds"),
        "folder": {"id": folder["id"], "name": folder.get("name", "")},
        "statuses": sorted(statuses.values(), key=lambda s: s["done"]),  # stable: keeps ClickUp's order
        "lists": lists,
    }
    snapshot["burndown"] = build_burndown(snapshot)  # projected schedule for the wheel page's burn-down table
    return snapshot


def main() -> None:
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("--out", type=Path, required=True)
    ap.add_argument("--folder", default=os.environ.get("CLICKUP_FOLDER_ID") or DEFAULT_FOLDER_ID)
    args = ap.parse_args()

    token = os.environ.get("CLICKUP_API_TOKEN", "").strip()
    if not token:
        sys.exit("CLICKUP_API_TOKEN is not set. Add it as a repository secret (see README.md on the gh-pages branch).")

    tracker_ids = json.loads((HERE / "tracker-ids.json").read_text(encoding="utf-8"))
    api = ClickUp(token)
    # The tasks are spread over folders and folderless lists of the "Dimensions Mod" space,
    # so collect every list in the space. Find the space through a tracker task, else the folder.
    space = {}
    for task_id in list(tracker_ids)[:3]:
        try:
            space = api.get(f"/task/{task_id}").get("space") or {}
        except SystemExit as e:
            print(f"Tracker task {tracker_ids[task_id]} not readable: {e}")
        if space.get("id"):
            break
    if not space.get("id"):
        space = api.folder(args.folder).get("space") or {}
    space_info = api.get(f"/space/{space['id']}")
    space["name"] = space.get("name") or space_info.get("name", "")
    lists = api.space_lists(space["id"])
    if not lists:
        sys.exit(f"No ClickUp lists found in space {space.get('name')} ({space.get('id')}); status.json left unchanged.")
    folder = {"id": space["id"], "name": space["name"], "lists": lists}
    tasks_by_list = {l["id"]: api.tasks(l["id"]) for l in folder.get("lists", [])}
    known = list(space_info.get("statuses") or [])
    for l in lists:  # lists can override the space's statuses
        known += [st for st in l.get("statuses") or [] if st.get("status") not in {k.get("status") for k in known}]
    snap = build_snapshot(folder, tasks_by_list, tracker_ids, known)

    args.out.parent.mkdir(parents=True, exist_ok=True)
    args.out.write_text(json.dumps(snap, indent=1), encoding="utf-8")
    total = sum(len(l["tasks"]) for l in snap["lists"])
    print(f"Wrote {args.out}: {len(snap['lists'])} lists, {total} tasks")


if __name__ == "__main__":
    main()
