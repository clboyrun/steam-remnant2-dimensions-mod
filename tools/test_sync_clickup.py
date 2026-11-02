import unittest

from sync_clickup import build_snapshot


def task(tid, name, status, stype, parent=None, deps=(), est=None, order=0):
    return {
        "id": tid,
        "name": name,
        "status": {"status": status, "color": "#000", "type": stype},
        "parent": parent,
        "time_estimate": est,
        "dependencies": [{"task_id": tid, "depends_on": d, "type": 1} for d in deps]
        + [{"task_id": "other", "depends_on": tid, "type": 1}],
        "linked_tasks": [{"task_id": tid, "link_id": "z"}, {"task_id": "y", "link_id": tid}],
        "orderindex": str(order),
        "date_updated": "1700000000000",
    }


class SnapshotTests(unittest.TestCase):
    def setUp(self):
        folder = {"id": "F", "name": "Remnant 2 Dimensions", "lists": [
            {"id": "L2", "name": "Second", "orderindex": 1},
            {"id": "L1", "name": "First", "orderindex": 0},
        ]}
        tasks = {
            "L1": [task("b", "B", "in progress", "custom", order=2), task("a", "A", "complete", "closed", est=7_200_000, order=1)],
            "L2": [task("c", "C", "to do", "open", deps=["b"]), task("d", "D", "to do", "open", deps=["a"], parent="c")],
        }
        known = [{"status": "to do", "color": "#111", "type": "open", "orderindex": 0},
                 {"status": "PROBLEM", "color": "#d33", "type": "custom", "orderindex": 1},
                 {"status": "in progress", "color": "#222", "type": "custom", "orderindex": 2},
                 {"status": "complete", "color": "#333", "type": "closed", "orderindex": 3}]
        self.snap = build_snapshot(folder, tasks, {"a": "E1"}, known)

    def test_lists_and_tasks_keep_clickup_order(self):
        self.assertEqual([l["name"] for l in self.snap["lists"]], ["First", "Second"])
        self.assertEqual([t["id"] for t in self.snap["lists"][0]["tasks"]], ["a", "b"])

    def test_fields(self):
        a = self.snap["lists"][0]["tasks"][0]
        self.assertTrue(a["done"])
        self.assertEqual(a["tracker_id"], "E1")
        self.assertEqual(a["estimate_hours"], 2)
        self.assertEqual(a["url"], "https://app.clickup.com/t/a")

    def test_dependencies_and_links(self):
        c, d = self.snap["lists"][1]["tasks"]
        self.assertEqual(c["waits_on"], ["b"])   # not "other", which waits on c
        self.assertEqual(d["waits_on"], ["a"])
        self.assertEqual(c["links"], ["y", "z"])

    def test_parent(self):
        self.assertEqual(self.snap["lists"][1]["tasks"][1]["parent"], "c")

    def test_status_counts(self):
        counts = [(s["status"], s["count"]) for s in self.snap["statuses"]]
        # Unused statuses (PROBLEM) are listed too, in ClickUp's order, done ones last.
        self.assertEqual(counts, [("to do", 2), ("PROBLEM", 0), ("in progress", 1), ("complete", 1)])
        self.assertTrue(self.snap["statuses"][-1]["done"])
        self.assertEqual([s["status"] for s in self.snap["statuses"] if s["open"]], ["to do"])


if __name__ == "__main__":
    unittest.main()
