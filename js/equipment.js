/* Equipment gallery: builds filter chips and cards from window.EQUIPMENT (js/equipment-data.js). */
(function () {
  "use strict";
  var data = window.EQUIPMENT || [], L = window.EQUIPMENT_LABELS || {};
  var gallery = document.getElementById("gallery");
  if (!gallery) return;
  var state = { slot: "all", world: "all", source: "all" };
  var groups = {};
  document.querySelectorAll("#gal-filters [data-group]").forEach(function (g) { groups[g.dataset.group] = g; });
  var count = document.getElementById("gal-count"), empty = document.getElementById("gal-empty");

  function el(tag, cls, text) { var e = document.createElement(tag); if (cls) e.className = cls; if (text != null) e.textContent = text; return e; }
  function label(group, v) { return (L[group] && L[group][v]) || v; }
  function uniq(key) { var seen = []; data.forEach(function (d) { if (seen.indexOf(d[key]) < 0) seen.push(d[key]); }); return seen; }

  function buildGroup(group, allText, values) {
    var box = groups[group]; if (!box) return;
    var opts = [["all", allText]].concat(values.map(function (v) { return [v, label(group, v)]; }));
    opts.forEach(function (o) {
      var b = el("button", "chipbtn", o[1]); b.type = "button"; b.dataset.value = o[0]; b.setAttribute("aria-pressed", String(o[0] === "all"));
      b.addEventListener("click", function () { state[group] = o[0]; sync(); apply(); });
      box.appendChild(b);
    });
  }
  function sync() {
    Object.keys(groups).forEach(function (g) {
      groups[g].querySelectorAll("button").forEach(function (b) { b.setAttribute("aria-pressed", String(b.dataset.value === state[g])); });
    });
  }

  function dd(parent, k, v) {
    var row = el("div", "kv"); row.appendChild(el("dt", null, k));
    var d = el("dd", v ? null : "unassigned", v || "Unassigned"); row.appendChild(d); parent.appendChild(row);
  }
  var cards = data.map(function (d) {
    var a = el("article", "item " + d.world);
    a.appendChild(el("span", "slot", d.slot));
    a.appendChild(el("h3", null, d.name));
    a.appendChild(el("p", "txt", d.text));
    var chips = el("ul", "chips");
    [["Type", d.type, ""], ["World", label("world", d.world), "t-world"], ["Source", label("source", d.source), "t-effect"]].forEach(function (c) {
      var li = el("li", c[2]); li.appendChild(el("i", null, c[0] + " ")); li.appendChild(document.createTextNode(c[1])); chips.appendChild(li);
    });
    a.appendChild(chips);
    var dl = el("dl", "where");
    if (d.where) dd(dl, "Obtained", d.where);
    dd(dl, "Dungeon", d.dungeon); dd(dl, "Boss", d.boss);
    a.appendChild(dl);
    gallery.appendChild(a);
    return a;
  });

  function apply() {
    var n = 0;
    cards.forEach(function (c, i) {
      var d = data[i];
      var show = (state.slot === "all" || d.slot === state.slot) && (state.world === "all" || d.world === state.world) && (state.source === "all" || d.source === state.source);
      c.hidden = !show; if (show) n++;
    });
    count.textContent = n; empty.hidden = n > 0;
  }

  buildGroup("slot", "All slots", uniq("slot"));
  buildGroup("world", "All worlds", ["grimm", "deep", "arcade", "hell", "ward13", "unassigned"].filter(function (v) { return uniq("world").indexOf(v) >= 0; }));
  buildGroup("source", "Any source", ["boss", "dungeon", "vendor", "crafting", "unassigned"].filter(function (v) { return uniq("source").indexOf(v) >= 0; }));
  document.getElementById("gal-reset").addEventListener("click", function () { state = { slot: "all", world: "all", source: "all" }; sync(); apply(); });
  apply();
})();
