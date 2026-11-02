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
        self.snap = build_snapshot(folder, tasks, {"a": "E1"})

    def test_lists_and_tasks_keep_clickup_order(self):
        self.assertEqual([l["name"] for l in self.snap["lists"]], ["First", "Second"])
        self.assertEqual([t["id"] for t in self.snap["lists"][0]["tasks"]], ["a", "b"])

    def test_fields(self):
        a = self.snap["lists"][0]["tasks"][0]
        self.assertTrue(a["done"])
        self.assertEqual(a["tracker_id"], "E1")
        self.assertEqual(a["estimate_hours"], 2)
        self.assertEqual(a["url"], "https://app.clickup.com/t/a")

    def test_parent(self):
        self.assertEqual(self.snap["lists"][1]["tasks"][1]["parent"], "c")

    def test_status_counts(self):
        counts = {s["status"]: s["count"] for s in self.snap["statuses"]}
        self.assertEqual(counts, {"complete": 1, "in progress": 1, "to do": 2})
        self.assertTrue(self.snap["statuses"][-1]["done"])


if __name__ == "__main__":
    unittest.main()
