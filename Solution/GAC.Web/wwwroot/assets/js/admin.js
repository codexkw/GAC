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
      // Remember which field this upload is for: closing the modal (or a stray
      // click) can null activeInput before the request resolves.
      var target = activeInput;
      var data = new FormData(uploadForm);
      data.append("json", "true");
      fetch("/Admin/Media/Upload", {
        method: "POST",
        headers: { "RequestVerificationToken": token ? token.value : "" },
        body: data
      })
        .then(function (r) {
          // A non-JSON body means the request never reached the upload action
          // (request too large, expired anti-forgery token, signed-out session
          // redirecting to the login page, or a 500). Surface it instead of
          // silently leaving the field on its old value.
          return r.text().then(function (body) {
            var res;
            try { res = JSON.parse(body); }
            catch (_) {
              throw new Error(
                "Upload failed (HTTP " + r.status + "). The file may be too " +
                "large, or your session/anti-forgery token expired — reload " +
                "the page and sign in again, then retry.");
            }
            if (!r.ok) throw new Error(res.error || ("Upload failed (HTTP " + r.status + ")."));
            return res;
          });
        })
        .then(function (res) {
          if (res.path && target) {
            target.value = res.path;
            loadGrid();
            close(); // confirm the selection so it's obvious the field was set
          } else if (res.error) {
            alert(res.error);
          }
        })
        .catch(function (err) {
          alert(err && err.message ? err.message : "Upload failed.");
        });
    });
  }
})();
