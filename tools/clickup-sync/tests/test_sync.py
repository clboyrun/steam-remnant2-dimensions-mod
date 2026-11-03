import json
import sys
import tempfile
import threading
import time
import unittest
from http.server import BaseHTTPRequestHandler, HTTPServer
from pathlib import Path
from urllib.parse import urlparse

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))
from clickup_sync import cache as C
from clickup_sync.api import Client, RateLimiter
from clickup_sync.config import Config
from clickup_sync.sync import Engine, QuietGate

TASK = {"id": "t1", "name": "Export: Rigs", "status": {"status": "to do"}, "description": "old",
        "markdown_description": "old", "date_updated": "1", "assignees": [], "tags": [], "list": {"id": "L1", "name": "Required data inputs"},
        "dependencies": [{"task_id": "t1", "depends_on": "t0"}], "time_estimate": 7200000, "priority": None,
        "start_date": None, "due_date": None}
STATE = {"calls": [], "tasks": {"t1": dict(TASK)}, "throttle": 1}


class H(BaseHTTPRequestHandler):
    def log_message(self, *a): pass

    def _send(self, code, obj, headers=None):
        b = json.dumps(obj).encode()
        self.send_response(code)
        for k, v in (headers or {}).items():
            self.send_header(k, v)
        self.send_header("Content-Length", str(len(b)))
        self.end_headers()
        self.wfile.write(b)

    def _route(self, method):
        u = urlparse(self.path)
        n = int(self.headers.get("Content-Length") or 0)
        body = json.loads(self.rfile.read(n)) if n else None
        STATE["calls"].append((method, u.path, body))
        if STATE["throttle"] > 0:
            STATE["throttle"] -= 1
            return self._send(429, {"err": "slow"}, {"Retry-After": "0"})
        p = u.path
        if p == "/team":
            return self._send(200, {"teams": [{"id": "T", "members": [{"user": {"id": 7, "username": "Astra"}}]}]})
        if p == "/team/T/space":
            return self._send(200, {"spaces": [{"id": "S", "name": "Dimensions Mod"}]})
        if p == "/space/S/list":
            return self._send(200, {"lists": [{"id": "L1", "name": "Required data inputs"}]})
        if p == "/space/S/folder":
            return self._send(200, {"folders": []})
        if p == "/list/L1/task" and method == "GET":
            return self._send(200, {"tasks": list(STATE["tasks"].values()), "last_page": True})
        if p == "/task/t1" and method == "PUT":
            t = STATE["tasks"]["t1"]
            if "status" in body: t["status"] = {"status": body["status"]}
            if "markdown_description" in body: t["markdown_description"] = body["markdown_description"]
            if "start_date" in body: t["start_date"] = body["start_date"]
            if "assignees" in body: t["assignees"] = [{"id": 7, "username": "Astra"}]
            t["date_updated"] = "2"
            return self._send(200, t)
        if p == "/task/t1" and method == "GET":
            return self._send(200, STATE["tasks"]["t1"])
        if p == "/list/L1/task" and method == "POST":
            n = len([k for k in STATE["tasks"] if k.startswith("n")]) + 1
            t = {"id": f"n{n}", "name": body["name"], "status": {"status": body.get("status", "to do")},
                 "markdown_description": body.get("markdown_description", ""), "date_updated": "5",
                 "parent": body.get("parent"), "list": {"id": "L1", "name": "Required data inputs"},
                 "assignees": [], "tags": [], "time_estimate": body.get("time_estimate")}
            STATE["tasks"][t["id"]] = t
            return self._send(200, t)
        if "/tag" in p:
            if method == "POST" and p.startswith("/task/") and p.split("/tag/")[-1] == "ms-new" and not STATE.get("tag_ok"):
                STATE["tag_ok"] = True
                return self._send(400, {"err": "Tag does not exist"})
            return self._send(200, {})
        if p.endswith("/dependency") or "/link/" in p:
            return self._send(200, {})
        if p.startswith("/task/n") and method == "GET":
            return self._send(200, STATE["tasks"][p.split("/")[2]])
        if p == "/task/t1/comment":
            return self._send(200, {"id": 1})
        self._send(404, {})

    do_GET = lambda s: s._route("GET")
    do_PUT = lambda s: s._route("PUT")
    do_POST = lambda s: s._route("POST")


class Tests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.srv = HTTPServer(("127.0.0.1", 0), H)
        threading.Thread(target=cls.srv.serve_forever, daemon=True).start()

    def setUp(self):
        STATE.update(calls=[], tasks={"t1": dict(TASK)}, throttle=1)
        self.tmp = tempfile.mkdtemp()
        self.cfg = Config(cache_dir=self.tmp, api_base=f"http://127.0.0.1:{self.srv.server_port}", space_ids="")
        self.eng = Engine(self.cfg, Client("k", self.cfg.api_base, RateLimiter(100), sleep=lambda s: None), lambda m: None)

    def test_pull_writes_file_and_survives_429(self):
        self.eng.pull()
        f = Path(self.tmp) / "Required data inputs" / "t1 - export-rigs.md"
        self.assertTrue(f.exists())
        fields, _, _ = C.parse(f.read_text())
        self.assertEqual(fields["status"], "to do")
        self.assertEqual(fields["depends_on"], ["t0"])
        self.assertEqual(fields["time_estimate_hours"], 2.0)
        self.assertTrue((Path(self.tmp) / "Required data inputs" / "_index.md").exists())

    def test_edit_push_close_notes_and_no_start_date(self):
        self.eng.pull()
        f = Path(self.tmp) / "Required data inputs" / "t1 - export-rigs.md"
        t = f.read_text().replace('status: "to do"', 'status: "complete"').replace('start_date: null', 'start_date: "2026-11-02"')
        t = t.replace('assignees: []', 'assignees: ["Astra"]')
        t = t.replace("## Post comment\n", "## Post comment\nSatisfied, inferred.\n")
        t = t.replace("## Agent notes\n", "## Agent notes\nevidence: review-open-tickets\n")
        f.write_text(t)
        STATE["calls"].clear()
        self.assertEqual(self.eng.push(), 1)
        put = next(b for m, p, b in STATE["calls"] if m == "PUT")
        self.assertEqual(put["status"], "complete")
        self.assertNotIn("start_date", put)
        self.assertEqual(put["assignees"], {"add": [7], "rem": []})
        self.assertTrue(any(p.endswith("/comment") for m, p, b in STATE["calls"]))
        fields, comment, notes = C.parse(f.read_text())
        self.assertEqual(comment, "")
        self.assertIn("evidence", notes)
        self.assertEqual(fields["status"], "complete")
        STATE["calls"].clear()
        self.eng.push()  # start_date still local-only; nothing further to send except none
        self.assertFalse([c for c in STATE["calls"] if c[0] == "PUT"])

    def test_notes_survive_pull_and_reset(self):
        self.eng.pull()
        f = Path(self.tmp) / "Required data inputs" / "t1 - export-rigs.md"
        f.write_text(f.read_text().replace("## Agent notes\n", "## Agent notes\nremember me\n"))
        STATE["tasks"]["t1"]["name"] = "Export: Rigs (renamed)"
        STATE["tasks"]["t1"]["date_updated"] = "3"
        self.eng.pull()
        files = list((Path(self.tmp) / "Required data inputs").glob("t1 - *.md"))
        self.assertEqual(len(files), 1)
        self.assertIn("remember me", files[0].read_text())
        self.eng.reset()
        self.eng.pull()
        self.assertIn("remember me", next((Path(self.tmp) / "Required data inputs").glob("t1 - *.md")).read_text())

    def test_local_edit_not_overwritten_by_pull(self):
        self.eng.pull()
        f = Path(self.tmp) / "Required data inputs" / "t1 - export-rigs.md"
        f.write_text(f.read_text().replace('status: "to do"', 'status: "problem"'))
        STATE["tasks"]["t1"]["date_updated"] = "9"
        self.eng.pull()
        self.assertIn('status: "problem"', f.read_text())

    def test_create_with_hierarchy_and_links(self):
        self.eng.pull()
        d = Path(self.tmp) / "Required data inputs"
        c = self.eng.cache
        c.write_new("Required data inputs", "Child card").write_text(
            c.write_new("Required data inputs", "Child card").read_text()
            .replace('parent: null', 'parent: "Parent card"').replace('depends_on: []', 'depends_on: ["t1"]')
            .replace('links: []', 'links: ["Parent card"]').replace('time_estimate_hours: null', 'time_estimate_hours: 3'))
        c.write_new("Required data inputs", "Parent card")
        STATE["calls"].clear()
        self.eng.push()
        posts = [(m, p, b) for m, p, b in STATE["calls"] if m == "POST"]
        creates = [b for m, p, b in posts if p == "/list/L1/task"]
        self.assertEqual([b["name"] for b in creates], ["Parent card", "Child card"])  # parent first
        self.assertEqual(creates[1]["parent"], "n1")
        self.assertEqual(creates[1]["time_estimate"], 10800000)
        self.assertIn(("POST", "/task/n2/dependency", {"depends_on": "t1"}), posts)
        self.assertIn(("POST", "/task/n2/link/n1", {}), posts)
        self.assertEqual(len(list(d.glob("NEW - *.md"))), 0)
        self.assertEqual(len(list(d.glob("n1 - *.md"))), 1)
        STATE["calls"].clear()
        self.eng.push()
        self.assertFalse([c for c in STATE["calls"] if c[0] in ("POST", "PUT")])

    def test_milestones_tags_and_close_assigns_astra(self):
        STATE["tasks"]["t1"]["tags"] = [{"name": "MS-Alpha"}]
        STATE["tag_ok"] = False
        self.eng.pull()
        ms = (Path(self.tmp) / "_milestones.md").read_text()
        self.assertIn("## ms-alpha (0/1 closed)", ms)
        c = self.eng.cache
        c.write_new("Required data inputs", "Milestone: New")
        m = Path(self.tmp) / "Required data inputs" / "NEW - milestone-new.md"
        m.write_text(m.read_text().replace('tags: []', 'tags: ["ms-new"]').replace('depends_on: []', 'depends_on: ["t1"]'))
        f = Path(self.tmp) / "Required data inputs" / "t1 - export-rigs.md"
        f.write_text(f.read_text().replace('status: "to do"', 'status: "complete"').replace('tags: ["ms-alpha"]', 'tags: ["ms-alpha", "ms-beta"]'))
        STATE["calls"].clear()
        self.eng.push()
        calls = [(m_, p_, b) for m_, p_, b in STATE["calls"]]
        put = next(b for m_, p_, b in calls if m_ == "PUT")
        self.assertEqual(put["assignees"], {"add": [7], "rem": []})
        self.assertIn(("POST", "/task/t1/tag/ms-beta", {}), calls)
        self.assertIn(("POST", "/space/S/tag", {"tag": {"name": "ms-new", "tag_fg": "#ffffff", "tag_bg": "#7b68ee"}}), calls)
        self.assertIn(("POST", "/task/n1/dependency", {"depends_on": "t1"}), calls)
        self.assertIn('tags: ["ms-new"]', next((Path(self.tmp) / "Required data inputs").glob("n1 - *.md")).read_text())

    def test_quiet_gate(self):
        now = [1000.0]
        g = QuietGate(self.tmp, 180, clock=lambda: now[0])
        self.assertFalse(g.poll())
        (Path(self.tmp) / "a.md").write_text("x")
        self.assertFalse(g.poll())
        now[0] += 179
        self.assertFalse(g.poll())
        (Path(self.tmp) / "a.md").write_text("xy")   # modify resets the timer
        self.assertFalse(g.poll())
        now[0] += 179
        self.assertFalse(g.poll())
        now[0] += 2
        self.assertTrue(g.poll())
        g.rebase()
        self.assertFalse(g.poll())
        (Path(self.tmp) / "a.md").unlink()           # delete counts too
        now[0] += 1
        self.assertFalse(g.poll())
        now[0] += 181
        self.assertTrue(g.poll())

    def test_rate_limiter(self):
        t = [0.0]
        slept = []
        def sl(s): slept.append(s); t[0] += s
        rl = RateLimiter(3, 60, clock=lambda: t[0], sleep=sl)
        for _ in range(4): rl.acquire()
        self.assertTrue(slept and 59 < sum(slept) < 61)


if __name__ == "__main__":
    unittest.main()
