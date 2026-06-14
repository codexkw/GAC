# GAC Motors Saudi — Bilingual CMS Design

**Date:** 2026-06-14
**Status:** Approved (design); pending implementation plan
**Repo:** https://github.com/codexkw/GAC.git

## 1. Goal

Replace the existing static HTML pixel-clone of GAC Motors Saudi with a custom
ASP.NET Core MVC application that:

1. Serves the **same pixel-perfect frontend**, now rendered server-side from a database.
2. Adds a **second language (Arabic, RTL)** modelled on `ar.gacmotorsaudi.com`, as a
   built-in dimension of the content model rather than a bolt-on.
3. Provides a **custom CMS** (admin panel) to manage all pages, content, media, and leads.

The reference sites are `en.gacmotorsaudi.com` and `ar.gacmotorsaudi.com`. The existing
static clone (the rendering target for visual parity) lives in
`C:\Users\anas-\source\repos\GAC\HTML` (~30 pages: 11 model pages + utility pages, shared
header/footer partials, `assets/css|js|img`, Node build scripts, `docs/research` with Arabic probes).

## 2. Locked Decisions

| Area | Decision |
|---|---|
| CMS platform | Custom ASP.NET Core MVC (no off-the-shelf CMS) |
| Solution structure | **Option A** — single web app + 2 class libraries (`GAC.Web`, `GAC.Core`, `GAC.Infrastructure`); admin as `Areas/Admin` |
| Content management | Structured content types (fixed layouts, field-based editing — not a free page builder) |
| Languages | English + Arabic, both stored per field; **cookie-based** language (clean URLs, no `/en` `/ar` prefix or subdomain) |
| RTL | `<html dir="rtl" lang="ar">` + layered `rtl.css` + Arabic webfont when culture = ar |
| Forms | Submissions stored in DB **and** emailed to dealership; managed in admin Leads inbox |
| Roles | Admin / Editor / Sales |
| Publishing | Edits go live immediately (no draft/preview/versioning) |
| Database | SQL Server `GAC` on `83.229.86.221,1433`; EF Core Code-First; connection string in `appsettings` |
| Hosting | IIS; migrations applied manually on prod; media in a configurable storage root (default `wwwroot/uploads`) |
| Solution location | `C:\Users\anas-\source\repos\GAC\Solution` (code + docs) |
| HTML reference | `C:\Users\anas-\source\repos\GAC\HTML` (visual/content source of truth) |

**Accepted consequence of cookie-based language:** each page has a single URL regardless of
language; search engines index whichever language is crawled and shared links do not carry
the language. The user chose this knowingly.

## 3. Solution Structure (Option A)

```
Solution/
  GAC.Web/            ASP.NET Core MVC — public site + Areas/Admin
  GAC.Core/           Domain entities, service interfaces + implementations, DTOs
  GAC.Infrastructure/ EF Core DbContext, migrations, email sender, file storage, seeders
  docs/superpowers/specs/   design docs
```

- Public site and admin share the same content services in `GAC.Core` — no duplication.
- `/admin` gated by ASP.NET Identity role policies; optionally IP-restricted at IIS.
- If admin isolation is later required, `GAC.Admin` can be split out cheaply because logic
  already lives in `GAC.Core`.

## 4. Content Model & Localization

### Localization primitive
Exactly two languages → EF Core **owned type**:

```
LocalizedText { string En; string Ar; }      // e.g. Vehicle.Tagline.En / .Ar
Localize(this LocalizedText)                   // returns the value for the current culture
```

No translation tables, no N-language overhead. Current culture comes from the language cookie.

### Entities
- **Vehicle** (11 model pages, template-driven) — `Slug`, `Category` (Sedan/SUV/EV, multi),
  `SortOrder`, `IsVisible`, `PriceFrom`, localized `Name`/`Tagline`/`IntroText`, `HeroImages[]`,
  `Trims[]` (name, price, highlights), `SpecGroups[]` → `SpecRows[]` (localized label/value),
  `ColorOptions[]` (name, hex, image), `GalleryImages[]`, `FeatureSections[]`
  (heading/body/image), `BrochurePdf`, localized SEO title/description.
- **ContentPage** (about, warranty, privacy-policy, finance, cost-of-service, road-assistance)
  — `Slug`, localized `Title`, ordered `ContentSections[]` (heading + rich-text body +
  optional image, all localized).
- **FormPage** (book-a-service, book-a-test-drive, request-a-quote, contact, fleet,
  recall-enquiry) — `Slug`, localized `Title` + intro copy, `FormType` enum selecting which
  coded form renders. Form fields are coded; only surrounding copy is editable.
- **Lead** — `FormType`, name, phone, email, message, related `Vehicle?`, preferred date,
  `SourcePage`, `Status` (New/Contacted/Closed), `CreatedAt`.
- **HomePage** (singleton) — hero `Slides[]` (image, heading, subheading, CTA text/link),
  featured vehicles, offers strip, news teasers.
- **NewsArticle** & **Offer** — editable content types (localized title, body, image, date).
- **SiteSettings** (singleton) — phone, WhatsApp, addresses, social links, footer columns,
  logos (localized where text).
- **MenuItem** — editable top-level nav + dropdown groups; the **megamenu vehicle grid
  auto-populates from visible Vehicles**.
- **MediaAsset** — metadata for uploads (path under storage root, localized alt text).
- **ApplicationUser / Roles** — ASP.NET Identity; roles Admin/Editor/Sales.

## 5. Public Rendering, Routing & Arabic/RTL

- **HTML → Razor.** Each static page ported to a Razor view. Client-side `includes.js`
  injection replaced by server-side rendering: `_Layout.cshtml` owns `<html dir lang>` + head +
  scripts; `_Header`/`_Footer` partials render from `SiteSettings` + `MenuItem`s + visible
  `Vehicle`s. **`main.js`, CSS, and all design assets stay unchanged** to preserve the look.
- **Routing (clean URLs, no `.html`):** `/` → HomePage; `/models` → listing; `/{slug}` →
  Vehicle; content pages, form pages, `/news`, `/offers` by slug. Old `*.html` paths
  301-redirect to clean equivalents.
- **Language & RTL:** `RequestLocalization` middleware (`en` + `ar`), culture from cookie;
  header toggle sets cookie + redirects back. Razor picks `.En`/`.Ar` and sets `dir`/`lang`.
  RTL = existing stylesheet + layered `rtl.css` + Arabic webfont (Cairo / Noto Kufi Arabic),
  matching `docs/research/arabic-home` captures. Numerals stay Latin by default.
- **Form submission flow:** anti-forgery + server-side validation → save `Lead` → SMTP
  notification → localized thank-you. Honeypot field + basic rate-limit for spam.

## 6. Admin Area

Under `/admin` (`Areas/Admin`), gated by Identity role policies. Login, seeded roles, admin
password reset.

| Section | Admin | Editor | Sales |
|---|:--:|:--:|:--:|
| Dashboard | ✓ | ✓ | ✓ |
| Vehicles | ✓ | ✓ | — |
| Content Pages | ✓ | ✓ | — |
| Home Page | ✓ | ✓ | — |
| News / Offers | ✓ | ✓ | — |
| Form Pages (copy only) | ✓ | ✓ | — |
| Menu / Navigation | ✓ | ✓ | — |
| Media Library | ✓ | ✓ | — |
| Leads inbox | ✓ | — | ✓ |
| Site Settings | ✓ | — | — |
| Users & Roles | ✓ | — | — |

- **Bilingual editing UX:** every localized field shows EN + AR side by side (or tabbed);
  prose uses a rich-text editor (TinyMCE/Quill) with an RTL-aware AR variant.
- **Vehicle editor:** repeatable add/remove/reorder sub-forms for trims, spec rows (grouped),
  color swatches, gallery, feature blocks.
- **Leads inbox:** filter by type/status/date, detail view, status changes, CSV export.
- **Media library:** upload to storage root (validation + resize), reusable image picker,
  EN/AR alt text.

## 7. Cross-Cutting Concerns

- **Auth & security:** Identity cookie auth, role policies, anti-forgery on all POST, lockout,
  HTTPS; existing `web.config` hardening headers carried over; `/admin` optionally IP-restricted.
- **Email:** `IEmailSender` abstraction, SMTP in `appsettings`; lead notification (optional
  customer auto-ack). **Graceful failure** — lead still saved + thank-you shown if SMTP fails.
- **Media/file storage:** configurable storage root (default `wwwroot/uploads`); settable to an
  absolute writable folder for the IIS app pool.
- **Config/secrets:** connection string + SMTP in `appsettings`.
- **SEO:** per-page localized title/meta description, `sitemap.xml`, `robots.txt`.
- **Performance:** in-memory cache for hot reads (SiteSettings, Menu, vehicle list), invalidated
  on save.
- **Errors & validation:** friendly localized 404/500 pages; localized validation messages.
- **Content seeding (migration bridge):** one-time seeder imports existing HTML content into the
  DB — 11 vehicles, content pages, menu, site settings — so the site renders identically on
  first run. EN seeded from current HTML; AR seeded from the `ar.gacmotorsaudi.com` reference
  where available, otherwise left for editors.
- **Testing:** focused unit tests for services + a few controller/form integration tests
  (kept lean).

## 8. Build Sequence

1. **Foundation** — `git init` + remote `codexkw/GAC`, scaffold Option A, EF Core + SQL Server
   `DbContext`, Identity + seeded roles, port HTML into `_Layout` + header/footer partials,
   localization middleware + cookie toggle.
2. **Content model** — all entities + `LocalizedText`, first migration, EN seeder.
3. **Public rendering** — port page bodies to Razor, wire content from DB, dynamic
   header/megamenu/footer, clean routing + old-`.html` redirects.
4. **Arabic / RTL** — `rtl.css`, Arabic webfont, AR seeding, full RTL parity pass.
5. **Forms & leads** — submission pipeline, `Lead` storage, SMTP, localized thank-you, spam guard.
6. **Admin area** — login, dashboard, bilingual CRUD for every type, media library, leads inbox +
   CSV, users/settings, role gating.
7. **Polish & QA** — SEO/sitemap, caching, error pages, tests, EN+AR visual parity vs. reference.
8. **Deploy** — publish to IIS, apply EF migrations manually on prod, run seeder, configure
   `appsettings` + SMTP.

## 9. Out of Scope (YAGNI)

- Draft/preview/publish workflow and content versioning (edits go live immediately).
- Per-language URLs, hreflang, language-specific SEO (cookie-based language chosen).
- More than two languages.
- Off-the-shelf CMS, headless/static publishing pipeline.
- Online payment / financing application processing (finance page is informational).
