"""Write a status snapshot of the Remnant 2 Dimensions ClickUp folder as JSON.

The GitHub Pages status page reads this file. It runs in a GitHub Action with the
ClickUp token in a repository secret, so the token never reaches the browser.

    CLICKUP_API_TOKEN=pk_... python tools/sync_clickup.py --out status.json

Only task names, statuses, hierarchy, estimates and dependencies are written.
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

API = "https://api.clickup.com/api/v2"
DEFAULT_FOLDER_ID = "901414897445"  # "Remnant 2 Dimensions" in the "Dimensions Mod" space
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
    waits_on = sorted(
        {d["depends_on"] for d in t.get("dependencies") or [] if d.get("task_id") == t["id"] and d.get("depends_on")}
    )
    return {
        "id": t["id"],
        "tracker_id": tracker_ids.get(t["id"]),
        "name": t.get("name", ""),
        "status": status.get("status", "unknown"),
        "status_color": status.get("color"),
        "done": is_done(status),
        "parent": t.get("parent"),
        "estimate_hours": round(int(estimate) / 3_600_000, 2) if estimate else None,
        "waits_on": waits_on,
        "url": t.get("url") or f"https://app.clickup.com/t/{t['id']}",
        "updated": int(t["date_updated"]) if t.get("date_updated") else None,
        "order": float(t.get("orderindex") or 0),
    }


def build_snapshot(folder: dict, tasks_by_list: dict[str, list[dict]], tracker_ids: dict[str, str]) -> dict:
    lists, statuses = [], {}
    for lst in sorted(folder.get("lists", []), key=lambda l: int(l.get("orderindex") or 0)):
        tasks = [task_record(t, tracker_ids) for t in tasks_by_list.get(lst["id"], [])]
        tasks.sort(key=lambda r: r["order"])
        for r in tasks:
            s = statuses.setdefault(r["status"], {"status": r["status"], "color": r["status_color"], "done": r["done"], "count": 0})
            s["count"] += 1
        lists.append({"id": lst["id"], "name": lst["name"], "tasks": tasks})

    # Blocked: waiting on a task that is not done yet.
    done = {r["id"]: r["done"] for l in lists for r in l["tasks"]}
    for l in lists:
        for r in l["tasks"]:
            r["blocked"] = not r["done"] and any(not done.get(d, False) for d in r["waits_on"])

    return {
        "generated_at": datetime.now(timezone.utc).isoformat(timespec="seconds"),
        "folder": {"id": folder["id"], "name": folder.get("name", "")},
        "statuses": sorted(statuses.values(), key=lambda s: (s["done"], -s["count"])),
        "lists": lists,
    }


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
    folder = api.folder(args.folder)
    folder["lists"] = api.lists(args.folder) or folder.get("lists") or []
    if not folder["lists"]:
        sys.exit(f"ClickUp folder {args.folder} ({folder.get('name')}) has no lists the token can see; status.json left unchanged.")
    tasks_by_list = {l["id"]: api.tasks(l["id"]) for l in folder.get("lists", [])}
    snap = build_snapshot(folder, tasks_by_list, tracker_ids)

    args.out.parent.mkdir(parents=True, exist_ok=True)
    args.out.write_text(json.dumps(snap, indent=1), encoding="utf-8")
    total = sum(len(l["tasks"]) for l in snap["lists"])
    print(f"Wrote {args.out}: {len(snap['lists'])} lists, {total} tasks")


if __name__ == "__main__":
    main()
