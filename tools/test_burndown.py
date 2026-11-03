import unittest

from burndown import build_burndown


def t(tid, est, waits=(), status="to do", done=False, open_=True):
    return {"id": tid, "tracker_id": tid.upper(), "name": f"Task {tid}", "status": status, "done": done, "open": open_,
            "estimate_hours": est, "waits_on": list(waits), "url": f"https://app.clickup.com/t/{tid}"}


def snap(*tasks):
    return {"lists": [{"id": "L", "name": "List", "tasks": list(tasks)}]}


def order(b):
    return [x["id"] for x in b["tickets"]]


class BurndownTests(unittest.TestCase):
    def test_blocked_ticket_waits_for_its_blocker_to_close(self):
        b = build_burndown(snap(t("b", 5, ["a"]), t("a", 10), t("c", 1)))
        self.assertEqual(order(b), ["a", "b", "c"])  # a unblocks the most work, so it goes first
        self.assertEqual(b["tickets"][1]["unblocked_by"][0]["id"], "a")

    def test_finished_blockers_do_not_block(self):
        b = build_burndown(snap(t("a", 3, done=True, status="complete", open_=False), t("b", 5, ["a"])))
        self.assertEqual(order(b), ["b"])

    def test_weeks_of_40_hours_split_long_tickets(self):
        b = build_burndown(snap(t("a", 30), t("b", 20)))
        self.assertEqual(b["week_count"], 2)
        self.assertEqual([s["hours"] for s in b["weeks"][0]["segments"]], [30, 10])
        self.assertEqual([s["hours"] for s in b["weeks"][1]["segments"]], [10])
        self.assertEqual(b["weeks"][0]["remaining_after"], 10)
        self.assertEqual((b["tickets"][1]["first_week"], b["tickets"][1]["last_week"]), (1, 2))

    def test_missing_estimate_is_flagged_and_holds_back_dependents(self):
        b = build_burndown(snap(t("a", None), t("b", 4, ["a"]), t("c", 2)))
        self.assertEqual(order(b), ["c"])
        self.assertEqual([x["id"] for x in b["unscheduled"]["no_estimate"]], ["a"])
        blocked = b["unscheduled"]["blocked"]
        self.assertEqual([x["id"] for x in blocked], ["b"])
        self.assertEqual(blocked[0]["because"], ["no estimate"])

    def test_cycle_is_reported_and_does_not_hang(self):
        b = build_burndown(snap(t("a", 1, ["b"]), t("b", 1, ["a"]), t("c", 1, ["a"]), t("d", 2)))
        self.assertEqual(order(b), ["d"])
        self.assertEqual(sorted(x["id"] for x in b["unscheduled"]["cycles"][0]), ["a", "b"])
        self.assertEqual([x["id"] for x in b["unscheduled"]["blocked"]], ["c"])

    def test_in_flight_tickets_go_first(self):
        b = build_burndown(snap(t("a", 9), t("b", 2, status="in process", open_=False)))
        self.assertEqual(order(b), ["b", "a"])
        self.assertTrue(b["tickets"][0]["in_flight"])

    def test_stuck_status_and_unknown_dependency_are_not_scheduled(self):
        b = build_burndown(snap(t("a", 2, status="problem", open_=False), t("b", 2, ["zzz"]), t("c", 1, ["a"])))
        self.assertEqual(order(b), [])
        self.assertEqual([x["id"] for x in b["unscheduled"]["stuck"]], ["a"])
        self.assertEqual(b["unscheduled"]["unknown_dependency"][0]["missing"], ["zzz"])
        self.assertEqual(b["unscheduled_count"], 3)


if __name__ == "__main__":
    unittest.main()
