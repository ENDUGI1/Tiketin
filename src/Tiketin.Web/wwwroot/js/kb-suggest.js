// Suggests knowledge-base articles while the user types a ticket title.
// Endpoint /api/v1/kb/articles?search= ships in a later milestone; until then
// the fetch 404s silently and the panel stays hidden.
(function () {
  "use strict";

  var input = document.getElementById("Input_Title");
  var panel = document.getElementById("kb-suggestions");
  var list = document.getElementById("kb-suggestions-list");
  if (!input || !panel || !list) return;

  var timer = null;
  var controller = null;

  input.addEventListener("input", function () {
    clearTimeout(timer);
    timer = setTimeout(query, 350);
  });

  function query() {
    var term = input.value.trim();
    if (term.length < 4) {
      hide();
      return;
    }

    if (controller) controller.abort();
    controller = new AbortController();

    fetch("/api/v1/kb/articles?search=" + encodeURIComponent(term) + "&pageSize=3", {
      headers: { Accept: "application/json" },
      signal: controller.signal
    })
      .then(function (res) { return res.ok ? res.json() : { data: [] }; })
      .then(function (body) { render(body.data || []); })
      .catch(function () { /* aborted or endpoint unavailable: keep quiet */ });
  }

  function render(articles) {
    if (!articles.length) {
      hide();
      return;
    }

    list.textContent = "";
    articles.forEach(function (article) {
      var li = document.createElement("li");
      var a = document.createElement("a");
      a.href = "/kb/" + article.slug;
      a.textContent = article.title;
      a.target = "_blank";
      a.rel = "noopener";
      li.appendChild(a);
      list.appendChild(li);
    });
    panel.hidden = false;
  }

  function hide() {
    panel.hidden = true;
    list.textContent = "";
  }
})();
