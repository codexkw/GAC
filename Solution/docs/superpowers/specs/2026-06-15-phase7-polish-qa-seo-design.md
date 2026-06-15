# GAC CMS — Phase 7: Polish, QA & SEO — Design Spec

**Date:** 2026-06-15
**Status:** Approved (design) — awaiting spec review before plan
**Phase:** 7 of 8 (final polish before Phase 8 Deploy)
**Repo:** codexkw/GAC (PUBLIC), branch `main`
**Stack:** ASP.NET Core 9 MVC, EF Core 9.0.6 (SQL Server), Razor + ViewComponents, IHtmlLocalizer + .resx, xUnit.

---

## 1. Goal

Make every public page search-engine- and share-ready (per-page localized SEO metadata, social cards,
structured data, sitemap/robots, a configurable analytics hook) and complete a bilingual
accessibility/QA review. This is the last polish before deploy.

**Key fact:** `MetaTitle`/`MetaDescription` (owned `LocalizedText`) **already exist** on `Vehicle`,
`ContentPage`, and `FormPage` and have been admin-editable since Phase 6a — but **nothing renders them
today**. `_Layout` currently emits only a static `<title>` suffix and one generic meta description.
Phase 7 finally consumes that data. **No new entities, no migration.**

## 2. Scope (confirmed)

In scope:
1. **SEO metadata pipeline** — per-page title / meta description / canonical / OpenGraph / Twitter card.
2. **sitemap.xml + robots.txt** — dynamic, DB-driven.
3. **JSON-LD structured data** — AutoDealer (global), Car (vehicle detail), NewsArticle (news detail).
4. **Configurable analytics hook** — GA4 / GTM snippet, off until an ID is configured.
5. **Accessibility + EN/AR QA pass** — audit-and-fix + checklist doc.

Out of scope (deferred / per master spec §9):
- **In-memory caching** — explicitly deferred (marginal benefit on a fast LAN DB; adds cache-staleness
  surface to every admin write path).
- Per-language URLs / `hreflang` — cookie-based language means one URL serves both; master spec lists this
  out of scope.
- Any schema change, new content type, or migration.

## 3. Architecture

Purely **additive** to the existing Option-A solution. No domain (`GAC.Core`) or EF (`GAC.Infrastructure`)
changes. All new code is in `GAC.Web`:

| Unit | Location | Responsibility |
|---|---|---|
| `SeoData` (POCO) | `GAC.Web/Models/SeoData.cs` | Presentation model carried via `ViewData["Seo"]`. |
| `SeoBuilder` | `GAC.Web/Infrastructure/SeoBuilder.cs` | Build absolute URLs + per-entity `SeoData` + JSON-LD strings. Testable. |
| `_SeoHead.cshtml` | `GAC.Web/Views/Shared/_SeoHead.cshtml` | Renders the SEO/OG/Twitter/JSON-LD tags from `SeoData`. |
| `SeoController` | `GAC.Web/Controllers/SeoController.cs` | `GET /sitemap.xml`, `GET /robots.txt`. |
| `AnalyticsOptions` | `GAC.Web/Infrastructure/AnalyticsOptions.cs` | Binds the `Analytics` config section. |
| Analytics snippet | inline in `_Layout.cshtml` | GA4/GTM, rendered only when an ID is set. |

Controllers that set `ViewData["Seo"]`: `HomeController`, `VehiclesController`, `PageController`
(content + vehicle + form branches), `NewsController` (Index + Detail), `OffersController`,
`FormsController`.

### 3.1 `SeoData` shape

```csharp
namespace GAC.Web.Models;

public sealed class SeoData
{
    public string? Title { get; set; }          // page title, BEFORE the " - GAC Mutawa Alkadi" suffix
    public string? Description { get; set; }     // meta description
    public string? CanonicalPath { get; set; }   // root-relative clean path, e.g. "/gs8"
    public string? OgImage { get; set; }         // root-relative or absolute image path; builder makes absolute
    public string OgType { get; set; } = "website";   // website | article | product
    public string? Robots { get; set; }          // e.g. "noindex,nofollow"; null => omit the tag
    public List<string> JsonLd { get; set; } = new();  // each entry is a complete JSON-LD object string
}
```

### 3.2 `_Layout` integration

`_Layout` `<head>` keeps its existing static `<title>`/description **only as the ultimate fallback** when
no `SeoData` is supplied; otherwise it delegates to `<partial name="_SeoHead" />`. Concretely: `_SeoHead`
reads `ViewData["Seo"] as SeoData` (or a default), and renders the full `<title>`, `<meta name=description>`,
canonical, OG, Twitter, robots, and any JSON-LD `<script>` blocks. The duplicate `<title>`/`<meta description>`
lines currently in `_Layout` are removed (moved into `_SeoHead`) so there is exactly one of each.

### 3.3 Absolute URLs

OpenGraph (`og:url`, `og:image`), canonical, sitemap entries, and `robots.txt`'s `Sitemap:` line need
absolute URLs. `SeoBuilder.AbsoluteUrl(HttpRequest req, string path)` composes `{scheme}://{host}{path}`
from the current request, so it is correct in dev and prod without hard-coding a domain.

## 4. Component detail

### 4.1 SEO metadata pipeline

**Rendered tags (`_SeoHead`):**
- `<title>{Title} - GAC Mutawa Alkadi</title>` (the suffix stays; `Title` may be a full MetaTitle).
- `<meta name="description" content="{Description}">`
- `<link rel="canonical" href="{absolute CanonicalPath}">`
- OpenGraph: `og:title`, `og:description`, `og:type`, `og:url`, `og:image` (absolute), `og:site_name`
  (= "GAC Mutawa Alkadi"), `og:locale` (`en` or `ar`).
- Twitter: `twitter:card=summary_large_image`, `twitter:title`, `twitter:description`, `twitter:image`.
- `<meta name="robots" content="{Robots}">` only when `Robots != null`.
- For each `JsonLd` entry: `<script type="application/ld+json">{entry}</script>`.

**Fallback chains** (all text via `.Localize()`):

| Page | Title | Description | OgImage | OgType |
|---|---|---|---|---|
| Vehicle detail | `MetaTitle ?? Name` | `MetaDescription ?? Tagline ?? IntroText ?? siteDefault` | hero image (`ThumbPath`) | `product` |
| Content page | `MetaTitle ?? Title` | `MetaDescription ?? siteDefault` | site default OG image | `website` |
| Form page | `MetaTitle ?? Title` | `MetaDescription ?? IntroText ?? siteDefault` | site default OG image | `website` |
| Home | site default title | siteDefault | site default OG image | `website` |
| `/models` | "Models" (localized) | siteDefault | site default OG image | `website` |
| News detail (`NewsArticle`) | `Title` | `Excerpt ?? siteDefault` | `ImagePath` | `article` |
| News/Offers list | localized label | siteDefault | site default OG image | `website` |

> Note: `ContentPage` has **no `IntroText`** field (only `Title`/`BodyHtml`/`MetaTitle`/`MetaDescription`/`Sections`),
> so its description falls back straight from `MetaDescription` to the site default. `Vehicle` and `FormPage`
> **do** have `IntroText`. `Vehicle` also has `Tagline`.

`siteDefault` description = the current generic string
(`"Discover the GAC Motor range — SUVs, sedans and EVs — from Mutawa Alkadi Automotive."`).
Site default OG image = `/assets/img/og-default.jpg` if present, else the SiteSettings logo, else the
favicon PNG. (A dedicated 1200×630 share image is a content nice-to-have — deferred item, not a blocker.)

`/not-found` sets `Robots = "noindex,nofollow"`. Form **thank-you** (PRG success) pages keep their normal
metadata (the URL is the same `/{slug}`).

### 4.2 sitemap.xml + robots.txt — `SeoController`

```
GET /sitemap.xml   → 200, Content-Type: application/xml; charset=utf-8
GET /robots.txt    → 200, Content-Type: text/plain; charset=utf-8
```

Both are attribute-routed literal paths (they beat the `/{slug}` catch-all the same way `/models` does).

**sitemap.xml** — a standard `<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">` containing:
- `/` (home), `/models`, `/news`, `/offers`
- every **visible** vehicle (`IVehicleService.GetVisibleAsync()`) → `/{slug}`
- every content page and form page → `/{slug}`
- every **published** news article → `/news/{slug}`

Each `<url>` has `<loc>` (absolute). `<lastmod>` is included **only for news articles** (from
`NewsArticle.PublishedOn`, a `DateOnly` → `yyyy-MM-dd`); no other content entity carries a timestamp, so
`<lastmod>` is omitted for them (a valid, common choice). Hidden vehicles and unpublished news are excluded
(the services already filter them).
URLs are absolute via `SeoBuilder.AbsoluteUrl`. The site has well under the 50,000-URL / 50 MB single-file
limit, so one sitemap file (no index) is correct.

**robots.txt**:
```
User-agent: *
Disallow: /admin
Disallow: /admin/

Sitemap: {absolute}/sitemap.xml
```
Dynamic (not a static file) so the `Sitemap:` host matches the serving environment.

### 4.3 JSON-LD structured data

Built in `SeoBuilder` and attached to `SeoData.JsonLd`:
- **AutoDealer** (home, and acceptable site-wide): `@type: AutoDealer`, `name` (constant "GAC Mutawa Alkadi"),
  `url` (absolute home), `logo` (absolute logo asset path), `telephone` (`SiteSettings.Phone`), `sameAs`
  (the non-empty social URLs: Instagram / Facebook / Tiktok / Snapchat / X). **No `address`** — `SiteSettings`
  has no address field, so it is omitted (added later if an address field is introduced).
- **Car** (vehicle detail): `@type: Car`, `name`, `image` (absolute hero), `brand: { @type: Brand, name: "GAC" }`,
  `description`.
- **NewsArticle** (news detail): `@type: NewsArticle`, `headline` (`Title`), `image` (absolute `ImagePath`),
  `datePublished` (`PublishedOn`, `yyyy-MM-dd`).

JSON is constructed via `System.Text.Json` serialization of anonymous/dictionary objects (proper escaping),
never string concatenation, to avoid injection/escaping bugs in the `<script>` block.

### 4.4 Analytics hook

New committed config in `appsettings.json` (non-secret, empty defaults):
```json
"Analytics": {
  "Ga4MeasurementId": "",
  "GtmContainerId": ""
}
```
`AnalyticsOptions` bound in `Program.cs` (`builder.Services.Configure<AnalyticsOptions>(...)`). `_Layout`
injects `IOptions<AnalyticsOptions>` and renders:
- If `GtmContainerId` non-empty → GTM `<script>` in `<head>` + `<noscript>` iframe immediately after `<body>`.
- Else if `Ga4MeasurementId` non-empty → GA4 `gtag.js` `<script>` in `<head>`.
- Else → nothing.

The IDs are non-secret; the real value is set per-environment at deploy (may live in
`appsettings.json` or environment config). Empty by default → no tracking emitted in dev or tests.

### 4.5 Accessibility + EN/AR QA pass

Audit-and-fix. Concrete checks and fixes:
- **Image alt text** — ensure every content image has a meaningful, localized `alt`: hero slides
  (`HeroSlide.Heading`), vehicle hero/gallery (vehicle `Name` + context), news (`NewsArticle.Title`) / offer
  images. Decorative images get `alt=""`.
- **Skip-to-content link** — add a visually-hidden "Skip to main content" anchor as the first focusable
  element, targeting the main content region.
- **Heading order & landmarks** — verify a single `<h1>` per page and a sensible heading hierarchy; ensure
  `<header>`/`<main>`/`<footer>` (or roles) exist. The public `RenderBody()` content should sit in a
  `<main id="...">` landmark that the skip link targets.
- **Contrast & focus** — spot-check brand colors against WCAG AA for body text; ensure visible focus styles.
- **EN + AR parity** — Lighthouse (or equivalent) run on home, `/models`, a vehicle detail, and `/contact-us`
  in both languages; confirm RTL parity vs the `HTML/` reference.
- **Deliverable:** fixes applied + `docs/superpowers/qa/2026-06-15-phase7-qa-checklist.md` recording what was
  checked, what was fixed, and residual notes.
- **Known false positive (do not chase):** the live tech/safety `h4` toggle headings render 0×0 and trip
  "missing label"/"empty heading" audits — this is expected behavior of the collapsed accordion, documented,
  not a defect.

## 5. Data flow

1. Request hits a public controller action.
2. The action loads its entity (existing services, unchanged) and calls `SeoBuilder` to produce a `SeoData`
   (incl. JSON-LD), then `ViewData["Seo"] = seo`.
3. `_Layout` renders `<partial name="_SeoHead" />`, which reads `ViewData["Seo"]` (or a default) and emits
   the head tags. Analytics snippet renders from `IOptions<AnalyticsOptions>`.
4. `SeoController` serves `/sitemap.xml` and `/robots.txt` independently of the view pipeline.

No caching, no new persistence, no migration.

## 6. Error handling

- `SeoBuilder` is null-safe: a null/blank field falls back down its chain; a page with no `SeoData` gets the
  site default (the current behavior, preserved).
- `sitemap.xml` building tolerates empty collections (still returns a valid empty-ish `<urlset>`).
- JSON-LD serialization uses `System.Text.Json` (correct escaping); a missing optional field is simply
  omitted from the object.
- Analytics renders nothing when IDs are empty — no broken `<script>`.

## 7. Testing

Integration tests via the existing `DevWebApplicationFactory` (real Development DB), plus focused unit tests
for `SeoBuilder` where it has no `HttpContext` dependency.

- **Sitemap:** `GET /sitemap.xml` → 200, `Content-Type` starts `application/xml`; body contains `<loc>` for
  home and a known visible vehicle slug (e.g. `/gs8`); body does **not** contain a hidden slug (e.g.
  `/aion-v`); body is well-formed XML (parses via `XDocument`).
- **Robots:** `GET /robots.txt` → 200, `text/plain`; body contains `Disallow: /admin` and a `Sitemap:` line
  ending `/sitemap.xml`.
- **SEO head:** a vehicle page response contains `rel="canonical"`, `property="og:title"`, and the page's
  `MetaTitle` when set (else `Name`); a `noindex` page (`/not-found`) contains `name="robots"`.
- **JSON-LD:** home response contains `"@type":"AutoDealer"`; a vehicle page contains `"@type":"Car"`.
- **Analytics:** under the default (empty) test config, a page response contains **no** `googletagmanager`
  / `gtag` script. (Optionally, a config-override test asserts presence when an ID is set.)
- **No regressions:** the full existing suite (171) stays green.

## 8. Build sequence (task outline for the plan)

1. `SeoData` POCO + `SeoBuilder` (absolute-URL + fallback helpers) with unit tests.
2. `_SeoHead` partial; wire `_Layout` to delegate; remove the duplicate static title/description.
3. Set `ViewData["Seo"]` in all public controllers (per the fallback table).
4. JSON-LD construction in `SeoBuilder` (AutoDealer / Car / NewsArticle) + render via `_SeoHead`.
5. `SeoController` — `sitemap.xml` + `robots.txt` + tests.
6. `AnalyticsOptions` + config section + `_Layout` snippet + test (absent by default).
7. Accessibility/QA pass — alt text, skip link, landmarks/headings, EN+AR Lighthouse, checklist doc.
8. Final review + HANDOFF/memory update.

## 9. Success criteria

- Every public page emits a correct, localized `<title>`, meta description, canonical, and OG/Twitter tags,
  using admin-editable `MetaTitle`/`MetaDescription` where set and sensible fallbacks otherwise.
- `/sitemap.xml` lists all indexable URLs (and only those) with absolute locs; `/robots.txt` disallows
  `/admin` and references the sitemap.
- Home, vehicle, and news pages carry valid JSON-LD.
- The analytics snippet is emitted only when an ID is configured.
- The accessibility pass is documented with fixes applied; EN+AR parity confirmed.
- Full test suite green (existing 171 + new Phase-7 tests).
- No schema change, no migration, no new secret in committed files.
