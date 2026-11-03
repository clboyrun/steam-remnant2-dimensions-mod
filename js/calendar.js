/* Heat map calendar for the wheel page. Each day is a square cut from top left to bottom right:
   top-right triangle = tickets open that day, bottom-left triangle = tickets closed that day.
   Both use the same 0 to 5 scale (5 or more is full colour). Dates come from status.json (created, start, closed). */
(function () {
  const MAX = 5;
  const DAY = 86400000;
  const esc = s => String(s).replace(/[&<>"']/g, c => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]));
  const dayStart = ms => { const d = new Date(ms); return new Date(d.getFullYear(), d.getMonth(), d.getDate()).getTime(); };
  const addDays = (t, n) => { const d = new Date(t); return new Date(d.getFullYear(), d.getMonth(), d.getDate() + n).getTime(); };
  const key = t => { const d = new Date(t); return d.getFullYear() + "-" + String(d.getMonth() + 1).padStart(2, "0") + "-" + String(d.getDate()).padStart(2, "0"); };
  const fmtDate = t => new Date(t).toLocaleDateString(undefined, { weekday: "short", year: "numeric", month: "short", day: "numeric" });
  const level = n => Math.min(n, MAX) / MAX;
  const MONTH = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];

  window.renderCalendar = function (data, includeClosed, mount) {
    const tasks = data.lists.flatMap(l => l.tasks);
    const now = Date.now(), today = dayStart(now);
    const dated = tasks.filter(t => t.created || t.start || t.closed);
    if (!dated.length) { mount.innerHTML = '<p class="note">Dates appear after the next hourly refresh.</p>'; return; }

    // Open-only view: only tickets that are still open count, so nothing is ever closed on the calendar.
    // Include closed: every ticket counts, with its full open span and its close day.
    const used = tasks.filter(t => (includeClosed || !t.done) && (t.created || t.start || t.closed));
    const open = {}, closed = {};
    used.forEach(t => {
      const from = dayStart(t.start || t.created || t.closed);
      let to = t.done ? dayStart(t.closed || t.updated || now) : today;
      if (to < from) to = from;
      for (let d = from; d <= to; d = addDays(d, 1)) (open[key(d)] = open[key(d)] || []).push(t);
      if (t.done && t.closed) (closed[key(dayStart(t.closed))] = closed[key(dayStart(t.closed))] || []).push(t);
    });

    const all = used.flatMap(t => [t.created, t.start, t.closed].filter(Boolean));
    let first = dayStart(Math.min.apply(null, all.length ? all : [today]));
    const last = today;
    // Weeks run Monday to Sunday; show at least five weeks so a young project doesn't look like a stub.
    const mondayOf = t => addDays(t, -((new Date(t).getDay() + 6) % 7));
    let gridStart = mondayOf(first);
    const weeks = Math.max(5, Math.ceil((last - gridStart) / DAY / 7 + (new Date(last).getDay() === 1 ? 1 : 0.01)));
    const cols = [];
    for (let w = 0; w < weeks; w++) {
      const days = [];
      for (let i = 0; i < 7; i++) days.push(addDays(gridStart, w * 7 + i));
      cols.push(days);
    }

    let monthRow = "", lastMonth = -1;
    cols.forEach(days => {
      const m = new Date(days[0]).getMonth();
      monthRow += "<span>" + (m !== lastMonth ? MONTH[m] : "") + "</span>";
      lastMonth = m;
    });
    const dow = ["Mon", "", "Wed", "", "Fri", "", "Sun"].map(d => "<span>" + d + "</span>").join("");

    const cells = cols.map(days => '<div class="cal-col">' + days.map(d => {
      const k = key(d), o = (open[k] || []).length, c = (closed[k] || []).length, future = d > today;
      const lbl = fmtDate(d) + ": " + o + " ticket" + (o === 1 ? "" : "s") + " open, " + c + " closed";
      return '<button type="button" class="cal-cell' + (future ? " future" : "") + (d === today ? " today" : "") + '" data-day="' + k + '" style="--o:' + (level(o) * 100).toFixed(0) + "%;--c:" + (level(c) * 100).toFixed(0) + '%" title="' + esc(lbl) + '" aria-label="' + esc(lbl) + '"' + (future ? " disabled" : "") + "></button>";
    }).join("") + "</div>").join("");

    const ramp = (cls, n) => '<i class="cal-sw ' + cls + '" style="--o:' + (level(n) * 100) + "%;--c:" + (level(n) * 100) + '%"></i>';
    const scale = cls => [0, 1, 2, 3, 4, 5].map(n => ramp(cls, n)).join("");
    mount.innerHTML =
      '<div class="cal-wrap"><div class="cal-dow" aria-hidden="true">' + dow + '</div><div class="cal-scroll"><div class="cal-months" style="--weeks:' + weeks + '" aria-hidden="true">' + monthRow + '</div><div class="cal-grid" style="--weeks:' + weeks + '">' + cells + "</div></div></div>" +
      '<div class="cal-legend"><span class="cal-key"><i class="cal-cellkey"></i></span>' +
      '<span><b>Top right</b>: tickets open that day (from start or created date, until closed) ' + scale("cal-sw-o") + "</span>" +
      '<span><b>Bottom left</b>: tickets closed that day ' + scale("cal-sw-c") + "</span>" +
      "<span>0 to 5 tickets; 5 or more is full colour for both</span></div>" +
      (includeClosed ? "" : '<p class="note">Only tickets that are still open are counted, so the bottom-left triangles stay empty. Tick “Include closed tickets” above the wheel to see completions and each finished ticket’s open days.</p>') +
      '<div class="cal-detail" id="cal-detail" aria-live="polite"><p class="note">Select a day to list its tickets.</p></div>';

    const detail = mount.querySelector("#cal-detail");
    const list = ts => ts.length ? "<ul>" + ts.map(t => '<li><span class="tid">' + esc(t.tracker_id || "·") + '</span> <a href="' + esc(t.url) + '" target="_blank" rel="noopener">' + esc(t.name) + "</a></li>").join("") + "</ul>" : '<p class="note">None.</p>';
    mount.querySelectorAll(".cal-cell").forEach(b => b.addEventListener("click", () => {
      mount.querySelectorAll(".cal-cell.sel").forEach(x => x.classList.remove("sel"));
      b.classList.add("sel");
      const k = b.dataset.day, o = open[k] || [], c = closed[k] || [];
      const t = new Date(k + "T00:00:00").getTime();
      detail.innerHTML = "<h3>" + esc(fmtDate(t)) + "</h3>" +
        "<h4>Closed (" + c.length + ")</h4>" + list(c) +
        "<h4>Open (" + o.length + ")</h4>" + (o.length > 40 ? '<p class="note">' + o.length + " tickets; first 40 shown.</p>" + list(o.slice(0, 40)) : list(o));
    }));
  };
})();
