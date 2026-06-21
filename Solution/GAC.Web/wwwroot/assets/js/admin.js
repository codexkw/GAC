// Admin panel scripts: reusable media picker.
(function () {
  "use strict";
  var modal = document.getElementById("mediaPicker");
  if (!modal) return;

  var grid = modal.querySelector("[data-picker-grid]");
  var token = modal.querySelector('input[name="__RequestVerificationToken"]');
  var activeInput = null;

  function open(input) { activeInput = input; modal.classList.add("is-open"); loadGrid(); }
  function close() { modal.classList.remove("is-open"); activeInput = null; }

  function loadGrid() {
    fetch("/Admin/Media/List", { headers: { "Accept": "application/json" } })
      .then(function (r) { return r.json(); })
      .then(function (items) {
        grid.innerHTML = "";
        items.forEach(function (it) {
          var isPdf = /\.pdf$/i.test(it.path);
          var el;
          if (isPdf) {
            el = document.createElement("div");
            el.className = "adm-picker-thumb adm-picker-thumb--pdf";
            el.title = it.path;
            el.textContent = "PDF: " + it.path.split("/").pop();
          } else {
            el = document.createElement("img");
            el.src = it.path; el.title = it.path; el.className = "adm-picker-thumb";
          }
          el.addEventListener("click", function () {
            if (activeInput) activeInput.value = it.path;
            close();
          });
          grid.appendChild(el);
        });
      });
  }

  document.addEventListener("click", function (e) {
    var pick = e.target.closest("[data-media-pick]");
    if (pick) { open(pick.parentElement.querySelector("[data-media-input]")); return; }
    if (e.target.closest("[data-picker-close]")) { close(); }
  });

  var uploadForm = modal.querySelector("[data-picker-upload]");
  if (uploadForm) {
    uploadForm.addEventListener("submit", function (e) {
      e.preventDefault();
      var data = new FormData(uploadForm);
      data.append("json", "true");
      fetch("/Admin/Media/Upload", {
        method: "POST",
        headers: { "RequestVerificationToken": token ? token.value : "" },
        body: data
      })
        .then(function (r) { return r.json(); })
        .then(function (res) {
          if (res.path && activeInput) { activeInput.value = res.path; loadGrid(); }
          else if (res.error) { alert(res.error); }
        });
    });
  }
})();
