/* Kuwait Branch — site scripts */

(function () {
  'use strict';

  /* ---------- Mobile drawer ---------- */
  const drawer = document.querySelector('[data-drawer]');
  const drawerOpen = document.querySelector('[data-drawer-open]');
  const drawerClose = document.querySelector('[data-drawer-close]');
  if (drawerOpen) drawerOpen.addEventListener('click', () => drawer.classList.add('is-open'));
  if (drawerClose) drawerClose.addEventListener('click', () => drawer.classList.remove('is-open'));
  if (drawer) drawer.addEventListener('click', (e) => { if (e.target === drawer) drawer.classList.remove('is-open'); });

  /* Drawer accordion groups (Models / Owners / Shopping Tools / More) */
  document.querySelectorAll('[data-drawer-group] > .drawer__toggle').forEach((btn) => {
    btn.addEventListener('click', () => {
      const group = btn.parentElement;
      const isOpen = group.classList.toggle('is-open');
      btn.setAttribute('aria-expanded', isOpen ? 'true' : 'false');
    });
  });

  /* ---------- Hero slider ---------- */
  const slides = document.querySelectorAll('[data-hero-slide]');
  const dots = document.querySelectorAll('[data-hero-dot]');
  const heroPrev = document.querySelector('[data-hero-prev]');
  const heroNext = document.querySelector('[data-hero-next]');
  let heroIdx = 0;
  let heroTimer = null;
  const HERO_INTERVAL = 6000;

  function activateSlide(i) {
    if (slides.length === 0) return;
    const n = slides.length;
    heroIdx = ((i % n) + n) % n;
    slides.forEach((s, idx) => s.classList.toggle('is-active', idx === heroIdx));
    dots.forEach((d, idx) => d.classList.toggle('is-active', idx === heroIdx));
  }
  function nextSlide() { activateSlide(heroIdx + 1); }
  function prevSlide() { activateSlide(heroIdx - 1); }
  function restartTimer() {
    if (heroTimer) clearInterval(heroTimer);
    heroTimer = setInterval(nextSlide, HERO_INTERVAL);
  }
  if (slides.length > 0) {
    activateSlide(0);
    dots.forEach((d, idx) => d.addEventListener('click', () => { activateSlide(idx); restartTimer(); }));
    if (heroNext) heroNext.addEventListener('click', () => { nextSlide(); restartTimer(); });
    if (heroPrev) heroPrev.addEventListener('click', () => { prevSlide(); restartTimer(); });
    heroTimer = setInterval(nextSlide, HERO_INTERVAL);
  }

  /* ---------- Tabs (model strip) ---------- */
  document.querySelectorAll('[data-tabs]').forEach((tabRoot) => {
    const btns = tabRoot.querySelectorAll('[data-tab-btn]');
    const panelRoot = tabRoot.parentElement.querySelector('[data-tab-root]') || document;
    const panels = panelRoot.querySelectorAll('[data-tab-panel]');
    btns.forEach((b) => {
      b.addEventListener('click', () => {
        const target = b.getAttribute('data-tab-btn');
        btns.forEach((x) => x.classList.toggle('is-active', x === b));
        panels.forEach((p) => p.classList.toggle('is-active', p.getAttribute('data-tab-panel') === target));
        // reset carousel position when switching tabs
        panels.forEach((p) => {
          const track = p.querySelector('.carousel__track');
          if (track) { track.style.transform = 'translateX(0)'; track.dataset.idx = '0'; }
        });
      });
    });
  });

  /* ---------- Carousels (model strip) ---------- */
  document.querySelectorAll('[data-carousel]').forEach((root) => {
    const track = root.querySelector('.carousel__track');
    const slides = track.querySelectorAll('.carousel__slide');
    const prev = root.querySelector('[data-carousel-prev]');
    const next = root.querySelector('[data-carousel-next]');
    if (!slides.length) return;
    track.dataset.idx = '0';

    function slidesPerView() {
      const w = window.innerWidth;
      if (w <= 520) return 1;
      if (w <= 860) return 2;
      if (w <= 1100) return 3;
      return 4;
    }
    function go(delta) {
      const per = slidesPerView();
      const maxIdx = Math.max(0, slides.length - per);
      let idx = parseInt(track.dataset.idx || '0', 10) + delta;
      if (idx < 0) idx = maxIdx;
      if (idx > maxIdx) idx = 0;
      track.dataset.idx = String(idx);
      const slide = slides[0];
      const slideW = slide.getBoundingClientRect().width;
      const gap = parseFloat(getComputedStyle(track).gap || '0');
      track.style.transform = `translateX(-${idx * (slideW + gap)}px)`;
    }
    if (prev) prev.addEventListener('click', () => go(-1));
    if (next) next.addEventListener('click', () => go(1));
    window.addEventListener('resize', () => { track.dataset.idx = '0'; track.style.transform = 'translateX(0)'; });
  });

  /* ---------- Filter chips (models page) ---------- */
  document.querySelectorAll('[data-filter-root]').forEach((root) => {
    const chips = root.querySelectorAll('[data-filter]');
    const items = root.querySelectorAll('[data-cat]');
    chips.forEach((c) => {
      c.addEventListener('click', () => {
        const f = c.getAttribute('data-filter');
        chips.forEach((x) => x.classList.toggle('is-active', x === c));
        items.forEach((it) => {
          const cats = (it.getAttribute('data-cat') || '').split(/\s+/);
          it.style.display = (f === 'all' || cats.includes(f)) ? '' : 'none';
        });
      });
    });
  });

  /* ---------- Simple form validation ---------- */
  document.querySelectorAll('[data-form]').forEach((form) => {
    form.addEventListener('submit', (e) => {
      e.preventDefault();
      let ok = true;
      form.querySelectorAll('[required]').forEach((input) => {
        const field = input.closest('.field');
        const valid = input.value.trim() !== '' && (input.type !== 'email' || /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(input.value));
        if (!valid) { field.classList.add('error'); ok = false; }
        else { field.classList.remove('error'); }
      });
      if (ok) {
        form.innerHTML = '<div style="padding:32px;text-align:center;border:1px solid var(--c-line);border-radius:8px;background:var(--c-bg-2)"><h3 style="margin-bottom:8px">Thanks — we received your request.</h3><p class="muted">A representative will contact you within one business day.</p></div>';
      }
    });
    form.querySelectorAll('[required]').forEach((input) => {
      input.addEventListener('input', () => input.closest('.field').classList.remove('error'));
    });
  });

  /* ---------- Back to top ---------- */
  const backTop = document.querySelector('[data-back-top]');
  if (backTop) {
    window.addEventListener('scroll', () => {
      backTop.classList.toggle('is-visible', window.scrollY > 400);
    });
  }

  /* ---------- Header scroll-shrink (collapse utility + brand rows) ---------- */
  const gacHeader = document.querySelector('[data-header]');
  if (gacHeader) {
    const onScroll = () => gacHeader.classList.toggle('is-shrunk', window.scrollY > 120);
    onScroll();
    window.addEventListener('scroll', onScroll, { passive: true });
  }

  /* ---------- MODELS mega-menu filter ---------- */
  document.querySelectorAll('[data-mm]').forEach((mm) => {
    const tabs = mm.querySelectorAll('[data-mm-tab]');
    const items = mm.querySelectorAll('[data-mm-cat]');
    tabs.forEach((t) => {
      t.addEventListener('click', (e) => {
        e.preventDefault();
        const f = t.getAttribute('data-mm-tab');
        tabs.forEach((x) => x.classList.toggle('is-active', x === t));
        items.forEach((it) => {
          const cats = (it.getAttribute('data-mm-cat') || '').split(/\s+/);
          it.classList.toggle('is-hidden', !(f === 'all' || cats.includes(f)));
        });
      });
    });
  });

  /* ---------- News carousel (auto-rotating, infinite, pager dots) ---------- */
  document.querySelectorAll('[data-news-carousel]').forEach((root) => {
    const track = root.querySelector('[data-news-track]');
    const prevBtn = root.querySelector('[data-news-prev]');
    const nextBtn = root.querySelector('[data-news-next]');
    const pager = root.querySelector('[data-news-pager]');
    if (!track) return;
    const originals = Array.from(track.children);
    const count = originals.length;
    if (count < 2) return;

    // Triple the slides for seamless infinite scroll: [clones][originals][clones]
    originals.forEach((s) => track.appendChild(s.cloneNode(true)));
    originals.slice().reverse().forEach((s) => track.insertBefore(s.cloneNode(true), track.firstChild));

    let idx = count;        // start on the first real slide
    let animating = false;
    let timer = null;
    const INTERVAL = 6000;

    function step() {
      const first = track.children[0];
      const w = first.getBoundingClientRect().width;
      const gap = parseFloat(getComputedStyle(track).gap || '0');
      return w + gap;
    }
    function apply(animate) {
      track.style.transition = animate ? 'transform .5s ease' : 'none';
      track.style.transform = 'translateX(-' + (idx * step()) + 'px)';
    }
    function setDots() {
      if (!pager) return;
      const active = ((idx - count) % count + count) % count;
      pager.querySelectorAll('.newscar__dot').forEach((d, i) => d.classList.toggle('is-active', i === active));
    }
    function move(delta) {
      if (animating || delta === 0) return;
      animating = true;
      idx += delta;
      apply(true);
      setDots();
    }
    track.addEventListener('transitionend', () => {
      if (idx >= count * 2) { idx -= count; apply(false); }
      else if (idx < count) { idx += count; apply(false); }
      animating = false;
    });
    function goToDot(i) {
      const active = ((idx - count) % count + count) % count;
      move(i - active);
    }
    function start() { stop(); timer = window.setInterval(() => move(1), INTERVAL); }
    function stop() { if (timer) { window.clearInterval(timer); timer = null; } }

    if (nextBtn) nextBtn.addEventListener('click', () => { move(1); start(); });
    if (prevBtn) prevBtn.addEventListener('click', () => { move(-1); start(); });
    if (pager) pager.querySelectorAll('.newscar__dot').forEach((d, i) =>
      d.addEventListener('click', () => { goToDot(i); start(); }));
    root.addEventListener('mouseenter', stop);
    root.addEventListener('mouseleave', start);
    window.addEventListener('resize', () => apply(false));

    apply(false);
    setDots();
    start();
  });

  /* ---------- Model page: single-image sliders (exterior / interior) ---------- */
  document.querySelectorAll('[data-slider]').forEach((root) => {
    const track = root.querySelector('[data-slider-track]');
    if (!track) return;
    const slides = track.children;
    if (slides.length < 2) return;
    let idx = 0;
    let timer = null;
    const restart = () => { if (timer) clearInterval(timer); timer = setInterval(() => go(1), 6000); };
    // build dot pager
    let pager = root.querySelector('[data-slider-pager]');
    if (!pager) { pager = document.createElement('div'); pager.className = 'mp-slider__pager'; root.appendChild(pager); }
    for (let i = 0; i < slides.length; i++) {
      const b = document.createElement('button');
      b.type = 'button';
      b.setAttribute('aria-label', 'Slide ' + (i + 1));
      b.addEventListener('click', () => { idx = i; apply(); restart(); });
      pager.appendChild(b);
    }
    const dots = pager.children;
    const apply = () => {
      track.style.transform = 'translateX(-' + (idx * 100) + '%)';
      for (let i = 0; i < dots.length; i++) dots[i].classList.toggle('is-active', i === idx);
    };
    const go = (d) => { idx = (idx + d + slides.length) % slides.length; apply(); };
    const prev = root.querySelector('[data-slider-prev]');
    const next = root.querySelector('[data-slider-next]');
    if (prev) prev.addEventListener('click', () => { go(-1); restart(); });
    if (next) next.addEventListener('click', () => { go(1); restart(); });
    root.addEventListener('mouseenter', () => { if (timer) clearInterval(timer); });
    root.addEventListener('mouseleave', restart);
    apply();
    restart();
  });

  /* ---------- Model page: gallery 3-up carousels ---------- */
  document.querySelectorAll('[data-gallery]').forEach((root) => {
    const track = root.querySelector('[data-gallery-track]');
    if (!track) return;
    const items = track.children;
    if (!items.length) return;
    let idx = 0;
    const per = () => { const w = window.innerWidth; return w <= 620 ? 1 : (w <= 980 ? 2 : 3); };
    const maxIdx = () => Math.max(0, items.length - per());
    const apply = () => {
      if (idx > maxIdx()) idx = maxIdx();
      const first = items[0];
      const gap = parseFloat(getComputedStyle(track).gap) || 0;
      const w = first.getBoundingClientRect().width;
      track.style.transform = 'translateX(-' + (idx * (w + gap)) + 'px)';
    };
    const prev = root.querySelector('[data-gallery-prev]');
    const next = root.querySelector('[data-gallery-next]');
    if (prev) prev.addEventListener('click', () => { idx = Math.max(0, idx - 1); apply(); });
    if (next) next.addEventListener('click', () => { idx = Math.min(maxIdx(), idx + 1); apply(); });
    window.addEventListener('resize', apply);
    apply();
  });

  /* ---------- Model page: card accordions (mobile) ---------- */
  document.querySelectorAll('.mp-card__title').forEach((t) => {
    t.addEventListener('click', () => {
      if (window.innerWidth > 620) return;
      const card = t.closest('.mp-card');
      if (card) card.classList.toggle('is-open');
    });
  });

  /* ---------- Model page: sub-nav scroll-spy ---------- */
  (function () {
    const links = document.querySelectorAll('.mp-subnav a');
    if (!links.length || !('IntersectionObserver' in window)) return;
    const map = {};
    links.forEach((a) => { const id = a.getAttribute('href'); if (id && id.charAt(0) === '#') map[id.slice(1)] = a; });
    const sections = Object.keys(map).map((id) => document.getElementById(id)).filter(Boolean);
    if (!sections.length) return;
    const io = new IntersectionObserver((entries) => {
      entries.forEach((e) => {
        if (e.isIntersecting) {
          links.forEach((l) => l.classList.remove('is-active'));
          if (map[e.target.id]) map[e.target.id].classList.add('is-active');
        }
      });
    }, { rootMargin: '-45% 0px -50% 0px', threshold: 0 });
    sections.forEach((s) => io.observe(s));
  })();

  /* ---------- Model page: colour picker (colorisers) ---------- */
  document.querySelectorAll('[data-colours]').forEach((root) => {
    const imgs = root.querySelectorAll('.mp-colours__img');
    const btns = root.querySelectorAll('[data-colour-btn]');
    btns.forEach((b) => {
      b.addEventListener('click', () => {
        const c = b.getAttribute('data-colour-btn');
        btns.forEach((x) => x.classList.toggle('is-active', x === b));
        imgs.forEach((im) => im.classList.toggle('is-active', im.getAttribute('data-colour') === c));
      });
    });
  });

  /* ---------- Model page: safety accordion (mobile only) ---------- */
  document.querySelectorAll('.mp-stoggle__head').forEach((head) => {
    head.addEventListener('click', () => {
      const item = head.closest('.mp-stoggle');
      const open = item.classList.toggle('is-open');
      head.setAttribute('aria-expanded', open ? 'true' : 'false');
    });
  });

  /* ---------- Model page: gallery lightbox viewer ---------- */
  (function () {
    const lb = document.querySelector('[data-lightbox]');
    if (!lb) return;
    const img = lb.querySelector('[data-lb-img]');
    const count = lb.querySelector('[data-lb-count]');
    let list = [], i = 0;
    const show = () => {
      const a = list[i];
      if (!a) return;
      img.src = a.getAttribute('href');
      const inner = a.querySelector('img');
      img.alt = inner ? inner.alt : '';
      if (count) count.textContent = (i + 1) + ' / ' + list.length;
    };
    const open = (gallery, idx) => {
      list = Array.prototype.slice.call(gallery.querySelectorAll('.mp-gshot'));
      i = idx; show();
      lb.classList.add('is-open'); lb.setAttribute('aria-hidden', 'false');
      document.body.style.overflow = 'hidden';
    };
    const close = () => {
      lb.classList.remove('is-open'); lb.setAttribute('aria-hidden', 'true');
      document.body.style.overflow = '';
    };
    const go = (d) => { if (!list.length) return; i = (i + d + list.length) % list.length; show(); };
    document.querySelectorAll('.mp-gallery').forEach((g) => {
      g.querySelectorAll('.mp-gshot').forEach((a, idx) => {
        a.addEventListener('click', (e) => { e.preventDefault(); open(g, idx); });
      });
    });
    const cl = lb.querySelector('[data-lb-close]');
    const pv = lb.querySelector('[data-lb-prev]');
    const nx = lb.querySelector('[data-lb-next]');
    if (cl) cl.addEventListener('click', close);
    if (pv) pv.addEventListener('click', (e) => { e.stopPropagation(); go(-1); });
    if (nx) nx.addEventListener('click', (e) => { e.stopPropagation(); go(1); });
    lb.addEventListener('click', (e) => { if (e.target === lb) close(); });
    document.addEventListener('keydown', (e) => {
      if (!lb.classList.contains('is-open')) return;
      if (e.key === 'Escape') close();
      else if (e.key === 'ArrowLeft') go(-1);
      else if (e.key === 'ArrowRight') go(1);
    });
  })();

  /* ---------- Footer year ---------- */
  const yr = document.querySelector('[data-year]');
  if (yr) yr.textContent = new Date().getFullYear();

})();
