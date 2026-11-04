/* Sticky tab bar on the About page: highlights the section in view and keeps the active tab scrolled into the bar. */
(function () {
  "use strict";
  var bar = document.querySelector(".tabs-scroll");
  if (!bar) return;
  var tabs = Array.prototype.slice.call(bar.querySelectorAll("a[data-tab]"));
  var sections = tabs.map(function (t) { return document.getElementById(t.dataset.tab); });
  var current = null;

  function setActive(id) {
    if (id === current) return; current = id;
    tabs.forEach(function (t) {
      var on = t.dataset.tab === id;
      if (on) t.setAttribute("aria-current", "true"); else t.removeAttribute("aria-current");
      if (on) {
        var l = t.offsetLeft - 16, r = t.offsetLeft + t.offsetWidth + 16;
        if (l < bar.scrollLeft) bar.scrollLeft = l; else if (r > bar.scrollLeft + bar.clientWidth) bar.scrollLeft = r - bar.clientWidth;
      }
    });
  }
  function update() {
    var line = document.querySelector(".tabs").offsetHeight + 24, id = null;
    sections.forEach(function (s) { if (s && s.getBoundingClientRect().top <= line) id = s.id; });
    if (window.innerHeight + window.scrollY >= document.documentElement.scrollHeight - 4) id = sections[sections.length - 1].id;
    setActive(id);
  }
  var ticking = false;
  window.addEventListener("scroll", function () { if (!ticking) { ticking = true; requestAnimationFrame(function () { ticking = false; update(); }); } }, { passive: true });
  window.addEventListener("resize", update);
  update();
})();
