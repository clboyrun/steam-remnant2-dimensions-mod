/* Mailing-list signup on the About page.
   The site is static, so something has to receive the form. Set ONE of these:
     ENDPOINT  a form service URL that accepts a JSON POST (Formspree, Web3Forms, Getform, an Apps Script web app, ...)
     MAILTO    an address; the visitor's mail app opens with the signup filled in
   While both are empty the form still validates, but tells the visitor signup is not switched on yet. */
(function () {
  "use strict";
  var CONFIG = { ENDPOINT: "", MAILTO: "" };

  var form = document.getElementById("signup-form");
  if (!form) return;
  var details = form.closest("details");
  var statusEl = document.getElementById("signup-status");
  var locked = form.querySelectorAll("input[data-locked]");

  /* Release and Full Game stay checked. */
  locked.forEach(function (box) {
    box.addEventListener("change", function () { box.checked = true; });
    box.addEventListener("click", function (e) { e.preventDefault(); box.checked = true; });
  });

  /* Region parent <-> children. */
  var parents = form.querySelectorAll("input[data-parent]");
  parents.forEach(function (parent) {
    var kids = form.querySelectorAll('[data-group="' + parent.dataset.parent + '"] input[type=checkbox]');
    function sync() {
      var on = Array.prototype.filter.call(kids, function (k) { return k.checked; }).length;
      parent.checked = on === kids.length;
      parent.indeterminate = on > 0 && on < kids.length;
    }
    parent.addEventListener("change", function () {
      kids.forEach(function (k) { k.checked = parent.checked; });
      parent.indeterminate = false;
    });
    kids.forEach(function (k) { k.addEventListener("change", sync); });
    sync();
  });

  /* Open the panel when linked to (#signup). */
  function openFromHash() {
    if (location.hash === "#signup" && details) details.open = true;
  }
  openFromHash();
  window.addEventListener("hashchange", openFromHash);

  function say(msg, kind) {
    statusEl.textContent = msg;
    statusEl.className = "status" + (kind ? " " + kind : "");
  }

  function values(name) {
    return Array.prototype.map.call(form.querySelectorAll('input[name="' + name + '"]:checked'), function (b) { return b.value; });
  }

  form.addEventListener("submit", function (e) {
    e.preventDefault();
    var nameInput = form.elements.player;
    var player = nameInput.value.trim();
    if (!player) {
      say("Please enter your player name.", "bad");
      nameInput.focus();
      return;
    }
    if (form.elements.website.value) return; /* honeypot */
    locked.forEach(function (b) { b.checked = true; });

    var payload = {
      player: player,
      contact: form.elements.contact.value.trim(),
      builds: values("build"),
      topics: values("topic"),
      page: location.href,
      sent: new Date().toISOString()
    };

    if (CONFIG.ENDPOINT) {
      say("Sending…");
      fetch(CONFIG.ENDPOINT, {
        method: "POST",
        headers: { "Content-Type": "application/json", "Accept": "application/json" },
        body: JSON.stringify(payload)
      }).then(function (r) {
        if (!r.ok) throw new Error(r.status);
        say("Thanks, " + player + ". You're on the list.", "ok");
        form.reset();
      }).catch(function () {
        say("That didn't go through. Please try again in a moment.", "bad");
      });
    } else if (CONFIG.MAILTO) {
      var lines = [
        "Player name: " + payload.player,
        "Contact: " + (payload.contact || "(the address this is sent from)"),
        "Builds: " + payload.builds.join(", "),
        "Topics: " + payload.topics.join(", ")
      ];
      location.href = "mailto:" + encodeURIComponent(CONFIG.MAILTO) + "?subject=" + encodeURIComponent("Dimensions mailing list: " + player) +
        "&body=" + encodeURIComponent(lines.join("\n"));
      say("Your mail app should open with the signup ready to send.", "ok");
    } else {
      say("Signup isn't switched on yet. Check back soon.", "bad");
    }
  });
})();
