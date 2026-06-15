# Phase 7 — Accessibility QA Checklist

Date: 2026-06-15
Scope: GAC CMS public site (ASP.NET Core 9 MVC), Task 7 (accessibility audit + fixes).

## Code-level fixes applied (verified by reading rendered markup + green test suite)

- **Skip-to-content link.** Added `<a href="#main-content" class="skip-link">@L["Skip to main content"]</a>`
  as the first focusable element of `<body>` in `Views/Shared/_Layout.cshtml`, placed after the
  Task-6 GTM `<noscript>` block and before `<vc:header />`. Renders the Arabic string in AR and the
  literal English fallback in EN.
- **`.skip-link` CSS.** Appended to `wwwroot/assets/css/styles.css`. The link is visually hidden
  (`position:absolute; left:-9999px`) until it receives keyboard focus (`:focus { left:0 }`), at which
  point it becomes visible at the top-left with a white background / black text.
- **`<main id="main-content">` landmark.** `@RenderBody()` is now wrapped in
  `<main id="main-content">…</main>` in `_Layout.cshtml`, giving the page a single main landmark and a
  valid skip-link target. The back-to-top anchor, `main.js` script, and `Scripts` section at the bottom
  of the layout are unchanged.
- **Arabic skip-link string.** Added `<data name="Skip to main content">` →
  `تخطَّ إلى المحتوى الرئيسي` to `Resources/SharedResource.ar.resx`. No English `.resx` change is
  needed — the key is the English source text and is used as the fallback.
- **Image `alt` audit.** Audited every requested view. All meaningful content `<img>` elements already
  carry localized, descriptive `alt` text from earlier phases; no `<img>` was missing alt, so no markup
  changes were required (see table below). No class / id / `data-*` attributes were touched in any view.

### Landmarks / language already present (not changed here)

- `<html lang="@culture" dir="@dir">` is set from `CultureInfo.CurrentUICulture` (Phase 4) — `lang`/`dir`
  flip correctly between `en`/`ltr` and `ar`/`rtl`.
- Header and footer landmarks are emitted by the `<vc:header />` / `<vc:footer />` view components.

### Image alt audit results (per view)

| View | Images present | Status |
|------|----------------|--------|
| `Views/Home/Index.cshtml` | Category-carousel vehicle `<img>` (ALL/Sedan/SUV/EV) | Already `alt="@v.Name.Localize()"`. Hero slides + news cards use CSS `background-image` (no `<img>`). No change. |
| `Views/Vehicles/Index.cshtml` | Lineup-card `<img>` | Already `alt="@v.Name.Localize()"`. No change. |
| `Views/News/Index.cshtml` | None (`background-image` only) | No `<img>`. No change. |
| `Views/News/Detail.cshtml` | Article hero `<img>` | Already `alt="@Model.Title.Localize()"`. No change. |
| `Views/Offers/Index.cshtml` | None (text-only offer cards) | No `<img>`/images. No change. |

## Automated checks

- **Build:** `dotnet build Solution/GAC.sln -c Debug` → **Build succeeded, 0 warnings, 0 errors.**
- **Tests:** `dotnet test Solution/GAC.sln` → **Passed! Failed: 0, Passed: 201, Skipped: 0, Total: 201.**
  Existing markup assertions (`mp-hero`, `dir-grid`, SEO/sitemap/analytics suites) remain green; the new
  `<main>` wrapper did not remove any body content markers the tests check.

## Remaining MANUAL steps for the human (NOT performed here)

> Lighthouse was **not** run. Claude cannot launch a browser or run Lighthouse; the steps below are
> outstanding manual work and **no scores have been fabricated**.

1. Run **Lighthouse** (Chrome DevTools) — Accessibility + SEO categories — on each of:
   `/`, `/models`, `/gs8`, `/contact-us`, in **both** languages (toggle language via the header).
2. Confirm a **single `<h1>` per page** and a sensible heading order (no skipped levels).
3. Confirm a **visible focus ring** when tabbing to the skip link (it should appear top-left on focus)
   and across the primary navigation.
4. Confirm **RTL parity** against the `HTML/` reference clone in Arabic (layout mirrors correctly,
   sliders still scroll the right direction).
5. **Record the Lighthouse scores** for each page/language here once the runs are complete.

### Known false positive (do not chase)

> **Known false positive (do not chase):** the live tech/safety `h4` toggle headings render 0×0 in the
> collapsed accordion and trip "empty heading"/"missing label" audits. This is expected behavior of the
> collapsed accordion, not a defect.
