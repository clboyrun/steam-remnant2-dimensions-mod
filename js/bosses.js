/* World filter for the boss cards on the About page. */
(function () {
  "use strict";
  var buttons = document.querySelectorAll(".filters [data-filter]");
  var cards = document.querySelectorAll(".boss");
  if (!buttons.length) return;
  buttons.forEach(function (btn) {
    btn.addEventListener("click", function () {
      var f = btn.dataset.filter;
      buttons.forEach(function (b) { b.setAttribute("aria-pressed", String(b === btn)); });
      cards.forEach(function (c) { c.hidden = f !== "all" && c.dataset.world !== f; });
    });
  });
})();
