/* GAC clone — shared chrome injector.
   Loaded in place of main.js. Fetches the single header/footer partials,
   injects them into the [data-include] placeholders, marks the active nav
   item for the current page, then loads main.js so its drawer / megamenu /
   slider handlers bind to the freshly-injected markup. Edit the header or
   footer once in partials/ and every page reflects it. */
(function () {
  'use strict';

  // Bump this when header/footer/main.js/styles change so browsers + CDN
  // fetch fresh copies instead of stale cached ones. Keep in sync with the
  // ?v= on the <link>/<script> tags in the HTML.
  var V = '3';

  function injectIncludes() {
    var slots = Array.prototype.slice.call(document.querySelectorAll('[data-include]'));
    return Promise.all(slots.map(function (el) {
      var name = el.getAttribute('data-include');
      return fetch('partials/' + name + '.html?v=' + V, { cache: 'no-cache' })
        .then(function (r) {
          if (!r.ok) throw new Error('include "' + name + '" -> HTTP ' + r.status);
          return r.text();
        })
        .then(function (html) {
          var tpl = document.createElement('template');
          tpl.innerHTML = html.trim();
          el.replaceWith(tpl.content);
        });
    }));
  }

  function currentFile() {
    var f = (location.pathname.split('/').pop() || '').toLowerCase();
    return f.indexOf('.html') !== -1 ? f : 'index.html';
  }

  function fileOf(a) {
    return (a.getAttribute('href') || '').split('/').pop().split('#')[0].toLowerCase();
  }

  function setActiveNav() {
    var file = currentFile();
    var header = document.querySelector('.gac-header');
    if (!header) return;

    // Top-level: highlight the item whose own href matches the page, OR whose
    // dropdown / megamenu contains a link to the current page.
    Array.prototype.forEach.call(header.querySelectorAll('.menu > li'), function (li) {
      var top = li.querySelector(':scope > a');
      if (!top) return;
      var match = fileOf(top) === file ||
        Array.prototype.some.call(li.querySelectorAll('a[href]'), function (a) { return fileOf(a) === file; });
      if (match) { top.classList.add('is-current'); top.setAttribute('aria-current', 'page'); }
    });

    // Sub-links inside dropdowns + megamenu: highlight the exact current page.
    Array.prototype.forEach.call(header.querySelectorAll('.drop a[href], .megamenu a[href]'), function (a) {
      if (fileOf(a) === file) a.classList.add('is-current');
    });
  }

  function loadMainScript() {
    var s = document.createElement('script');
    s.src = 'assets/js/main.js?v=' + V;
    document.body.appendChild(s);
  }

  function run() {
    injectIncludes()
      .then(setActiveNav)
      .then(loadMainScript)
      .catch(function (err) {
        console.error('[chrome-include]', err);
        loadMainScript(); // still load main.js so page-content scripts keep working
      });
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', run);
  } else {
    run();
  }
})();
