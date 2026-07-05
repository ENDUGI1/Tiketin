// Live SLA countdown for the technician queue.
// Elements carry data-sla-deadline (epoch ms) and data-sla-phase ("respon"/"selesai").
// Color shifts as the deadline nears: quiet -> warning (<25% window left, capped
// at 60 min) -> breach (past deadline, pulsing).
(function () {
  "use strict";

  var els = Array.prototype.slice.call(document.querySelectorAll("[data-sla-deadline]"));
  if (!els.length) return;

  var WARN_WINDOW_MS = 60 * 60 * 1000; // warn within the last hour

  function format(ms) {
    var total = Math.floor(Math.abs(ms) / 1000);
    var d = Math.floor(total / 86400);
    var h = Math.floor((total % 86400) / 3600);
    var m = Math.floor((total % 3600) / 60);
    var s = total % 60;
    if (d > 0) return d + "h " + h + "j"; // hari + jam
    if (h > 0) return h + "j " + pad(m) + "m";
    return m + "m " + pad(s) + "d";
  }

  function pad(n) {
    return (n < 10 ? "0" : "") + n;
  }

  function tick() {
    var now = Date.now();
    els.forEach(function (el) {
      var deadline = parseInt(el.getAttribute("data-sla-deadline"), 10);
      var phase = el.getAttribute("data-sla-phase");
      var remaining = deadline - now;

      el.classList.remove("sla--warning", "sla--breach");

      if (remaining <= 0) {
        el.classList.add("sla--breach");
        el.textContent = phase + " lewat " + format(remaining);
      } else {
        if (remaining < WARN_WINDOW_MS) {
          el.classList.add("sla--warning");
        }
        el.textContent = phase + " " + format(remaining);
      }
    });
  }

  tick();
  setInterval(tick, 1000);
})();
