/* Burn-down table for the wheel page. Draws data.burndown from status.json (built by tools/burndown.py). */
(function () {
  const esc = s => String(s).replace(/[&<>"']/g, c => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]));
  const fmt = h => (h >= 10 ? Math.round(h).toLocaleString() : (+h.toFixed(1)).toString());
  const label = s => { s = String(s); return s.charAt(0).toUpperCase() + s.slice(1).toLowerCase(); };

  // One colour per ticket: golden-angle hues, three lightness steps so neighbours stay apart. Uses the theme's --sat/--lit.
  const colour = i => "hsl(" + Math.round((i * 137.508) % 360) + " var(--sat) calc(var(--lit) + " + [0, 9, -9][i % 3] + "%))";
  const idOf = t => t.tracker_id || "·";
  const link = (t, text) => '<a href="' + esc(t.url) + '" target="_blank" rel="noopener">' + text + "</a>";

  window.renderBurndown = function (data, mount) {
    const b = data.burndown;
    if (!b || !b.tickets) { mount.innerHTML = '<p class="note">The burn-down appears after the next hourly refresh.</p>'; return; }
    const hpw = b.hours_per_week;
    const info = {};
    b.tickets.forEach((t, i) => { info[t.id] = Object.assign({ color: colour(i), n: i + 1 }, t); });

    const rows = b.weeks.map(w => {
      const segs = w.segments.map(s => {
        const t = info[s.id], wide = s.hours / hpw >= 0.06;
        const cont = (t.first_week < w.week ? "◂ " : "") + idOf(t) + (t.last_week > w.week ? " ▸" : "");
        const tip = idOf(t) + " · " + t.name + " · " + fmt(s.hours) + " h this week (" + fmt(t.hours) + " h total, weeks " + t.first_week + "–" + t.last_week + ")";
        return '<a class="seg" style="flex:' + s.hours + " 0 0;background:" + t.color + '" href="' + esc(t.url) + '" target="_blank" rel="noopener" title="' + esc(tip) + '" aria-label="' + esc(tip) + '">' +
          (wide ? "<span>" + esc(cont) + "</span>" : "") + "</a>";
      }).join("");
      const idle = hpw - w.hours;
      const pct = b.total_hours ? (w.remaining_after / b.total_hours) * 100 : 0;
      return "<tr><th scope=\"row\">" + w.week + '</th><td><div class="wk">' + segs +
        (idle > 0.01 ? '<span class="idle" style="flex:' + idle + ' 0 0" title="' + fmt(idle) + ' h unscheduled"></span>' : "") + "</div></td>" +
        '<td class="num">' + fmt(w.hours) + '</td><td class="num rem"><span class="remtrack"><span style="width:' + pct.toFixed(1) + '%"></span></span>' + fmt(w.remaining_after) + "</td></tr>";
    }).join("");

    const keyItems = b.tickets.map(t => {
      const i = info[t.id], by = t.unblocked_by.length ? " · ready after " + t.unblocked_by.map(x => esc(x.tracker_id || x.name)).join(", ") : (t.in_flight ? " · already in progress" : " · ready from the start");
      return '<li><i class="sw" style="background:' + i.color + '"></i><div><span class="tid">' + esc(idOf(t)) + "</span> " + link(t, esc(t.name)) + "</div>" +
        '<span class="h">' + fmt(t.hours) + " h · " + (t.first_week === t.last_week ? "week " + t.first_week : "weeks " + t.first_week + "–" + t.last_week) + esc(by) + "</span></li>";
    }).join("");

    const refs = list => list.map(x => '<li><span class="tid">' + esc(idOf(x)) + "</span> " + link(x, esc(x.name)) + "</li>").join("");
    const u = b.unscheduled, flags = [];
    if (u.no_estimate.length) flags.push("<h3>No estimate (" + u.no_estimate.length + ")</h3><p class=\"note\">Not scheduled and not guessed at. Anything waiting on these is held back too. Add an estimate in ClickUp and they join the next refresh.</p><ul>" + refs(u.no_estimate) + "</ul>");
    if (u.cycles.length) flags.push("<h3>Dependency cycles (" + u.cycles.length + ")</h3><p class=\"note\">These tickets wait on each other, so none can start. Remove one dependency in ClickUp to break the loop.</p>" +
      u.cycles.map(c => "<ul>" + refs(c) + "</ul>").join(""));
    if (u.stuck.length) flags.push("<h3>Stuck status (" + u.stuck.length + ")</h3><ul>" + u.stuck.map(x => '<li><span class="tid">' + esc(idOf(x)) + "</span> " + link(x, esc(x.name)) + " · " + esc(label(x.status)) + "</li>").join("") + "</ul>");
    if (u.unknown_dependency.length) flags.push("<h3>Waits on a ticket outside this snapshot (" + u.unknown_dependency.length + ")</h3><ul>" + refs(u.unknown_dependency) + "</ul>");
    if (u.blocked.length) flags.push("<h3>Held back by the above (" + u.blocked.length + ")</h3><ul>" + u.blocked.map(x =>
      '<li><span class="tid">' + esc(idOf(x)) + "</span> " + link(x, esc(x.name)) + " · waits on " + x.waiting_on.map(w => esc(idOf(w))).join(", ") + " (" + esc(x.because.join(", ")) + ")</li>").join("") + "</ul>");

    const weeks = b.week_count, years = weeks / 52;
    mount.innerHTML =
      '<div class="bd-sum"><span><b>' + weeks + "</b>weeks of " + hpw + ' h</span><span><b>' + fmt(b.total_hours) + ' h</b>scheduled</span><span><b>' + b.tickets.length + "</b>tickets</span>" +
      (b.unscheduled_count ? '<span class="warn"><b>' + b.unscheduled_count + "</b>not scheduled</span>" : "") + "</div>" +
      '<div class="bd-scroll"><table class="bd"><thead><tr><th scope="col">Week</th><th scope="col">Tickets worked (' + hpw + ' h per week; click one to open it in ClickUp)</th><th scope="col" class="num">Hours</th><th scope="col" class="num">Left after</th></tr></thead><tbody>' + rows + "</tbody></table></div>" +
      '<p class="note">◂ continues from the week before, ▸ continues into the next week. Roughly ' + years.toFixed(1) + ' years at one person and ' + hpw + ' h a week.</p>' +
      '<details class="bd-key" open><summary>Key: all ' + b.tickets.length + " scheduled tickets, in the order they are worked</summary><ol>" + keyItems + "</ol></details>" +
      (flags.length ? '<div class="bd-flags"><h2>Not scheduled</h2>' + flags.join("") + "</div>" : "");
  };
})();
