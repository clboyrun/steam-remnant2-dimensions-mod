"""Project a burn-down schedule from a status snapshot.

Called by sync_clickup.py when it builds status.json, so the schedule is rebuilt on every refresh
and the page only has to draw it. It reads the snapshot only; nothing is written to ClickUp.

The simulation (one worker, sequential, 40 hours per week):

1. Tickets already in flight (a status that is neither To do nor done, e.g. "in process") go first, in
   ClickUp order. Their full estimate is used because ClickUp does not say how far along they are.
2. Then, repeatedly: take the tickets that are *ready* (To do, every blocker finished, an estimate), pick
   the one with the most unfinished work waiting behind it, work it for its estimated hours, and treat it
   as closed when those hours are reached. Closing it can make more tickets ready, which are
   re-evaluated before the next pick.
3. Hours are laid out in weeks of 40; a ticket longer than the room left in a week continues into the next.

Never scheduled, and reported instead of guessed at: tickets with no estimate (and what waits on them),
tickets in a dependency cycle (and what waits on them), tickets whose status says they are stuck, and
tickets that wait on something not in the snapshot.
"""

from __future__ import annotations

HOURS_PER_WEEK = 40.0
STUCK_WORDS = ("problem", "block", "hold", "stuck")  # statuses that never count as progressing
EPS = 1e-9


def _flat(snapshot: dict) -> list[dict]:
    out = []
    for lst in snapshot.get("lists", []):
        for t in lst.get("tasks", []):
            out.append({**t, "list": lst.get("name", "")})
    return out


def _cycle_members(open_ids: set[str], deps: dict[str, list[str]]) -> list[list[str]]:
    """Strongly connected components with more than one ticket (or a self-wait), via Tarjan."""
    index, low, on, stack, comps, counter = {}, {}, set(), [], [], [0]

    def visit(v: str) -> None:
        # iterative to stay clear of the recursion limit
        work = [(v, iter(deps.get(v, ())))]
        index[v] = low[v] = counter[0]
        counter[0] += 1
        stack.append(v)
        on.add(v)
        while work:
            node, it = work[-1]
            advanced = False
            for w in it:
                if w not in open_ids:
                    continue
                if w not in index:
                    index[w] = low[w] = counter[0]
                    counter[0] += 1
                    stack.append(w)
                    on.add(w)
                    work.append((w, iter(deps.get(w, ()))))
                    advanced = True
                    break
                if w in on:
                    low[node] = min(low[node], index[w])
            if advanced:
                continue
            work.pop()
            if work:
                parent = work[-1][0]
                low[parent] = min(low[parent], low[node])
            if low[node] == index[node]:
                comp = []
                while True:
                    w = stack.pop()
                    on.discard(w)
                    comp.append(w)
                    if w == node:
                        break
                if len(comp) > 1 or node in deps.get(node, ()):
                    comps.append(comp)

    for v in sorted(open_ids):
        if v not in index:
            visit(v)
    return comps


def build_burndown(snapshot: dict, hours_per_week: float = HOURS_PER_WEEK) -> dict:
    tasks = _flat(snapshot)
    by_id = {t["id"]: t for t in tasks}
    pos = {t["id"]: i for i, t in enumerate(tasks)}  # list order, then ClickUp order within a list
    todo = [t for t in tasks if not t["done"]]
    todo_ids = {t["id"] for t in todo}

    def stuck(t: dict) -> bool:
        return any(w in t["status"].lower() for w in STUCK_WORDS)

    def estimate(t: dict) -> float | None:
        h = t.get("estimate_hours")
        return h if h and h > 0 else None

    deps = {t["id"]: list(t.get("waits_on") or []) for t in tasks}
    dependents: dict[str, list[str]] = {}
    for tid in todo_ids:
        for d in deps[tid]:
            dependents.setdefault(d, []).append(tid)

    # Reasons a ticket can never be scheduled by itself.
    unknown = {tid: [d for d in deps[tid] if d not in by_id] for tid in todo_ids}
    cycles = _cycle_members(todo_ids, deps)
    in_cycle = {m for c in cycles for m in c}
    no_estimate = {t["id"] for t in todo if estimate(t) is None}
    stuck_ids = {t["id"] for t in todo if stuck(t)}

    def downstream_hours(tid: str) -> float:
        seen, stack, total = {tid}, [tid], 0.0
        while stack:
            for c in dependents.get(stack.pop(), ()):
                if c not in seen:
                    seen.add(c)
                    stack.append(c)
                    total += estimate(by_id[c]) or 0.0
        return total

    candidates = [t for t in todo
                  if t["id"] not in no_estimate and t["id"] not in in_cycle
                  and t["id"] not in stuck_ids and not unknown[t["id"]]]
    weight = {t["id"]: downstream_hours(t["id"]) for t in candidates}

    finished = {t["id"] for t in tasks if t["done"]}
    order: list[dict] = []  # scheduled tickets, in the order they are worked
    clock = 0.0

    def schedule(t: dict, unblocked_by: list[str]) -> None:
        nonlocal clock
        h = estimate(t)
        order.append({"id": t["id"], "hours": h, "start_h": clock, "end_h": clock + h,
                      "in_flight": not t["open"], "unblocked_by": unblocked_by, "pick": len(order) + 1})
        clock += h
        finished.add(t["id"])

    left = {t["id"] for t in candidates}
    for t in sorted((t for t in candidates if not t["open"]), key=lambda t: pos[t["id"]]):
        if all(d in finished or d in left for d in deps[t["id"]]):
            schedule(t, [])
            left.discard(t["id"])
    while True:
        ready = [tid for tid in left if all(d in finished for d in deps[tid])]
        if not ready:
            break
        pick = min(ready, key=lambda tid: (-weight[tid], pos[tid]))
        blockers = [d for d in deps[pick] if d in by_id and not by_id[d]["done"]]
        # "Unblocked by" is whichever blocker was worked last; empty when it was ready from the start.
        last = max(blockers, key=lambda d: next((o["end_h"] for o in order if o["id"] == d), -1), default=None)
        schedule(by_id[pick], [last] if last else [])
        left.discard(pick)

    # Lay the hours out in weeks.
    weeks: list[dict] = []
    for o in order:
        start, end, remaining = o["start_h"], o["end_h"], o["hours"]
        cursor = start
        while remaining > EPS:
            w = int(cursor // hours_per_week + EPS)
            room = (w + 1) * hours_per_week - cursor
            take = min(room, remaining)
            while len(weeks) <= w:
                weeks.append({"week": len(weeks) + 1, "segments": []})
            weeks[w]["segments"].append({"id": o["id"], "hours": round(take, 2)})
            cursor += take
            remaining -= take
        o["first_week"] = int(start // hours_per_week + EPS) + 1
        o["last_week"] = int((end - EPS) // hours_per_week) + 1
    total = clock
    used = 0.0
    for w in weeks:
        w["hours"] = round(sum(s["hours"] for s in w["segments"]), 2)
        used += w["hours"]
        w["remaining_after"] = round(max(total - used, 0.0), 2)

    scheduled = {o["id"] for o in order}
    ref = lambda tid: {"id": tid, "tracker_id": by_id[tid].get("tracker_id") if tid in by_id else None,
                       "name": by_id[tid]["name"] if tid in by_id else tid,
                       "url": by_id[tid]["url"] if tid in by_id else f"https://app.clickup.com/t/{tid}"}
    # Everything not scheduled, with the first reason that applies.
    unscheduled = {"no_estimate": [], "cycles": [], "stuck": [], "unknown_dependency": [], "blocked": []}
    for tid in sorted(no_estimate, key=pos.get):
        unscheduled["no_estimate"].append(ref(tid))
    for comp in sorted(cycles, key=lambda c: min(pos[m] for m in c)):
        unscheduled["cycles"].append([ref(m) for m in sorted(comp, key=pos.get)])
    for tid in sorted(stuck_ids, key=pos.get):
        unscheduled["stuck"].append({**ref(tid), "status": by_id[tid]["status"]})
    for tid in sorted(todo_ids, key=pos.get):
        if unknown[tid]:
            unscheduled["unknown_dependency"].append({**ref(tid), "missing": unknown[tid]})
    reasons = {}
    for tid in todo_ids - scheduled:
        if tid in no_estimate or tid in in_cycle or tid in stuck_ids or unknown[tid]:
            continue
        waiting = [d for d in deps[tid] if d not in finished]
        why = []
        for d in waiting:
            if d in no_estimate:
                why.append("no estimate")
            elif d in in_cycle:
                why.append("dependency cycle")
            elif d in stuck_ids:
                why.append("stuck status")
            elif d in by_id and not by_id[d]["done"]:
                why.append("itself blocked")
            else:
                why.append("unknown ticket")
        reasons[tid] = (waiting, why)
    for tid in sorted(reasons, key=pos.get):
        waiting, why = reasons[tid]
        unscheduled["blocked"].append({**ref(tid), "waiting_on": [ref(d) for d in waiting if d in by_id],
                                       "because": sorted(set(why))})

    tickets = []
    for o in order:
        t = by_id[o["id"]]
        tickets.append({**ref(o["id"]), "list": t["list"], "status": t["status"], "hours": o["hours"],
                        "in_flight": o["in_flight"], "pick": o["pick"],
                        "first_week": o["first_week"], "last_week": o["last_week"],
                        "unblocked_by": [ref(d) for d in o["unblocked_by"]]})

    return {
        "hours_per_week": hours_per_week,
        "total_hours": round(total, 2),
        "week_count": len(weeks),
        "tickets": tickets,
        "weeks": weeks,
        "unscheduled": unscheduled,
        "unscheduled_count": len(todo_ids - scheduled),
    }
