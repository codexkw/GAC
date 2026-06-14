# GAC Motors Bilingual CMS — Handoff

**Last updated:** 2026-06-15 (end of Phase 4)
**Repo:** https://github.com/codexkw/GAC.git (PUBLIC) · branch `main`
**Stack:** ASP.NET Core 9 MVC, EF Core 9.0.6 (SQL Server), Razor + ViewComponents, IHtmlLocalizer + .resx, xUnit.

---

## 1. What this is

A custom bilingual (English + Arabic, RTL) CMS that replaces the original static HTML pixel-clone of
`en/ar.gacmotorsaudi.com`. Frontend renders server-side from the database; an admin panel (later phase)
will manage all content. Built in **8 phases**; **Phases 1–3 are complete and pushed.**

| Decision | Choice |
|---|---|
| Platform | Custom ASP.NET Core MVC (no off-the-shelf CMS) |
| Structure | **Option A**: `GAC.Web` (public + future `Areas/Admin`) + `GAC.Core` (domain) + `GAC.Infrastructure` (EF) + `GAC.Tests` |
| Content | Structured content types (field-based, not a page builder) |
| Languages | EN + AR per-field via EF owned type `LocalizedText{En,Ar}`; **cookie-based** language (clean URLs, no `/en` `/ar`) |
| RTL | `<html dir lang>` + layered `rtl.css` + Arabic webfont (Phase 4) |
| DB | SQL Server `GAC` @ `83.229.86.221,1433`, EF Code-First, **migrations applied manually** |
| Hosting | IIS; media in configurable storage root |
| Publishing | Edits go live immediately (no draft/versioning) |
| Roles | Admin / Editor / Sales |

Full design spec: `docs/superpowers/specs/2026-06-14-gac-cms-bilingual-design.md`.

---

## 2. Repo layout & secrets

```
GAC/
  HTML/        Original static clone — the visual/content SOURCE OF TRUTH for porting (HTML/docs, HTML/shots gitignored)
  Solution/
    GAC.Web/            MVC app (public site; Areas/Admin in Phase 6)
    GAC.Core/           Domain: Content/ entities, Services/ interfaces, Identity/ (NO EF dependency)
    GAC.Infrastructure/ EF: Data/ (DbContext, Configurations, Migrations, Seeders), Services/ (impls)
    GAC.Tests/          xUnit (unit + WebApplicationFactory integration)
    docs/               specs, plans, this HANDOFF
```

**SECRETS (repo is PUBLIC — do not break this):**
- Committed `GAC.Web/appsettings.json` holds a PLACEHOLDER: `Password=__SET_LOCALLY__`.
- The REAL connection string lives ONLY in `GAC.Web/appsettings.Development.json`, which is **gitignored**.
- Integration tests boot via `DevWebApplicationFactory` (`UseEnvironment("Development")`) to load the real conn string.
- The `sa` password is SHARED across other prod DBs (Pickly, White-Stiches) — rotating it is recommended defense-in-depth.
- **Never** `git add appsettings.Development.json`; always use scoped `git add`, never `git add -A`.

---

## 3. How to run / build / test

From repo root `C:\Users\anas-\source\repos\GAC`:
```bash
dotnet build Solution/GAC.sln -c Debug
dotnet test  Solution/GAC.sln        # 60 tests; integration tests need DB reachable
dotnet run --project Solution/GAC.Web # serves the site; Development env loads real conn string
```
- App **seeds at startup** (idempotent): `DbSeeder` (roles + admin) then `ContentSeeder` (EN content). It does **NOT** run migrations.
- Apply migrations manually: `dotnet ef database update --project Solution/GAC.Infrastructure --startup-project Solution/GAC.Web`.
- Seeded admin login: `admin@gacsaudi.local` / `ChangeMe!2026` (**change before go-live**).
- Language toggle: header switch → `CultureController.Set` writes the culture cookie.

---

## 4. Phase status

- **Phase 1 — Foundation** ✅ (solution scaffold, EF + Identity, seeded roles/admin, chrome ported, cookie localization). `InitialIdentity` migration applied to live DB.
- **Phase 2 — Content model** ✅ (18 entities + `LocalizedText`, `AddContentModel` migration applied to live DB, idempotent EN seeder). 15 tests.
- **Phase 3 — Public rendering & routing** ✅. 60 tests.
- **Phase 4 — Arabic / RTL** ✅ (this handoff). 67 tests.
- **Phase 5 — Forms & leads** ⏭️ NEXT · **Phase 6 — Admin area** · **Phase 7 — Polish/QA/SEO** · **Phase 8 — Deploy** — pending.

---

## 5. Phase 3 — what was built

Plan: `docs/superpowers/plans/2026-06-14-phase3-public-rendering.md`. Commits `55550e9..06205fc`.

**Service layer** (`GAC.Core/Services` interfaces, `GAC.Infrastructure/Services` impls, DI in `Program.cs`):
- `ISiteService` — `GetSettingsAsync()` (never null), `GetMenuAsync()` (top-level + ordered children).
- `IVehicleService` — `GetVisibleAsync()` (excludes hidden, ordered, +Images), `GetBySlugAsync()` (visible only, all children).
- `IContentService` — home, content page, form page, published news (+by slug), active offers.
- `Localize()` extension on `LocalizedText?` reads `CurrentUICulture` (ar→Arabic else English, null-safe).

**Chrome** — `HeaderViewComponent` / `FooterViewComponent` (`<vc:header/>`/`<vc:footer/>` in `_Layout`):
nav from `MenuItem`s, megamenu grid from visible vehicles, brandbar/socials from `SiteSettings`.
Helpers in `GAC.Web/Infrastructure/UrlHelpers.cs`: `NormalizeUrl` (legacy `.html`→clean), `CategoryCss`
(flags → `"suv ev"`), `ThumbPath` (first Gallery image else Hero).

**Routing** — see the map in §6. Catch-all `PageController.Show("/{slug}")` resolves
ContentPage → FormPage → visible Vehicle → 404. `LegacyHtmlRedirectMiddleware` (registered **before**
`UseStaticFiles`) 301-redirects `*.html`→clean. `UseStatusCodePagesWithReExecute("/not-found")` renders a 404 view.

**Pages** — all ~30 ported to Razor:
- Home (`Views/Home/Index.cshtml`): hero slider, four per-category tab-panel carousels, news cards — all DB-bound.
- `/models` listing (`Views/Vehicles/Index.cshtml`) from visible vehicles (filter chips via `data-cat`).
- Per-slug body partials rendered by container views:
  - `Views/Content/Page.cshtml` → `Views/Content/Pages/_{slug}.cshtml` (6: about, warranty, privacy-policy, finance, cost-of-service, road-assistance)
  - `Views/Forms/Page.cshtml` → `Views/Forms/Forms/_{slug}.cshtml` (6: book-a-service, book-a-test-drive, request-a-quote, contact-us, fleet, recall-enquiry)
  - `Views/Vehicles/Detail.cshtml` → `Views/Vehicles/Models/_{slug}.cshtml` (9 visible; hero image + name bound, rest verbatim)
- `/news` list + `/news/{slug}` detail; `/offers` (static marketing cards).

**Seeder update** (`75692eb`): Menu/HeroSlide URLs now clean; each vehicle gets a Gallery thumbnail
(`m-*` image); `news`/`offers` removed from `ContentPages` (owned by dedicated controllers).

---

## 5b. Phase 4 — Arabic / RTL — what was built

Plan: `docs/superpowers/plans/2026-06-15-phase4-arabic-rtl.md`. The site is now fully bilingual: language toggles via the cookie (no URL change), `<html dir lang>` flips, and Arabic renders RTL with the Cairo webfont. Verified visually EN+AR on home, `/models`, `/gs8` (mp-* detail), `/contact-us`; 67 tests green.

- **Static UI string localization** — `IHtmlLocalizer<SharedResource>` (`AddLocalization(ResourcesPath="Resources")` + `AddViewLocalization()`; injected globally as `@L` in `_ViewImports`). The marker type `SharedResource` lives in the **assembly-root namespace `GAC.Web`** (NOT `GAC.Web.Resources`) so the base name resolves to `GAC.Web.Resources.SharedResource` — putting it inside a `Resources` namespace doubles the path and silently breaks resolution. Keys ARE the English source text; only `Resources/SharedResource.ar.resx` exists (missing key → English). Used for chrome (Header/Footer) + small view strings (Explore, Read More, tabs, search, hero CTA fallback).
- **Arabic DB content** — `ContentSeeder.EnsureArabicAsync` (runs at end of `SeedAsync`, every startup): idempotent backfill that sets a `LocalizedText`'s `_Ar` **only when blank**, matched by natural key (slug / SortOrder / English label). Works on the already-seeded dev DB AND fresh DBs; never clobbers admin-edited Arabic (Phase 6). Covers all 8 content types + `Vehicle.Tagline`. All public views already routed text through `.Localize()` in Phase 3, so nothing else was needed.
- **RTL stylesheet + font** — `_Layout` loads Cairo + `rtl.css` only when `isRtl`. `rtl.css` mirrors the physical-direction rules from `styles.css` under `[dir="rtl"]` (text-align, chrome positioning, badges, borders, action-dock/back-top side, mp-* rules). **Sliders:** `main.js` uses LTR `translateX(-Npx)` math — instead of editing JS, the carousel/news/mp-slider tracks get `direction: ltr` (slide content restored to `rtl`), so the math stays correct. The hero is an opacity crossfade (no translate), so it needs no fix.
- **What stays English (by scope decision):** the deep hardcoded marketing prose inside the ported `Views/Vehicles/Models/_*.cshtml`, `Views/Content/Pages/_*.cshtml`, `Views/Forms/Forms/_*.cshtml` partials (and their breadcrumbs/section headings). Only their DB-bound `@Model.Title.Localize()` headings + chrome are Arabic. RTL layout still applies. These become editable/translatable via the Phase-6 admin.

---

## 6. Routing map

| URL | Handler | Source |
|---|---|---|
| `/` | `HomeController.Index` | `Views/Home/Index.cshtml` |
| `/models` | `VehiclesController.Index` | `Views/Vehicles/Index.cshtml` |
| `/news`, `/news/{slug}` | `NewsController` | `Views/News/Index.cshtml`, `Detail.cshtml` |
| `/offers` | `OffersController.Index` | `Views/Offers/Index.cshtml` |
| `/{slug}` (catch-all) | `PageController.Show` → ContentPage \| FormPage \| visible Vehicle \| 404 | container view → `_{slug}` partial |
| `/not-found` | `HomeController.NotFoundPage` | `Views/Shared/NotFound.cshtml` |
| `*.html` | `LegacyHtmlRedirectMiddleware` | 301 → clean URL |
| `/Culture/Set` | `CultureController.Set` | sets language cookie |

Precedence: literal attribute routes (`/models`, `/news`, `/offers`, `/not-found`) and the conventional
route (`/`, `/Culture/Set`) beat the single-segment `/{slug}`. Hidden vehicles (`aion-v`, `aion-es`,
`IsVisible=false`) have no partial and return 404 (service filters them out — no 500).

---

## 7. Conventions & gotchas (reuse in later phases)

**Porting a static HTML page → Razor partial** (the Phase-3 ruleset; reuse for Phase-4 AR work):
1. Transcribe **body only** — drop `<!doctype>`/`<head>`/`<body>`, the `data-include="header|footer"`
   placeholders, the back-to-top anchor, and **all `<script>`** (chrome + `main.js` live in `_Layout`).
2. `assets/...` → root-absolute `/assets/...` (incl. inline `background-image:url('assets/...')` — Razor
   `~/` does **not** resolve inside CSS `url()`). `pdfs/...` → `/pdfs/...`.
3. Internal `*.html` links → clean: strip `.html`; `index.html`→`/`; `contact.html`→`/contact-us`;
   `model-detail.html`→`/models`. Leave `#`, `tel:`, `https://` untouched.
4. Preserve **every** CSS class / id / `data-*` / inline style — `main.js` keys off them (megamenu
   `data-mm-*`, drawer `data-drawer*`, `mp-` sliders, `.mp-tabs`, `data-lightbox`, `.mp-stoggle`).
5. Forms render **STATIC** until Phase 5 — no `asp-for` / `method=post` / `asp-action` / anti-forgery.
6. Razor: literal `@` must be `@@` (watch Google-Maps `/@lat,long` URLs, emails).

**Other gotchas:**
- `<partial name="...">` throws a **500 on a missing view** (not 404) — every seeded slug MUST have a partial.
- `dotnet add package` floats Microsoft.*/EF to net10 previews — **pin to `9.0.*`** (currently 9.0.6).
- Singletons `SiteSettings`/`HomePage` are fetched via `FirstOrDefault`, not `Id==1`.
- EF: put `Include`/`ThenInclude` before `Where`/`OrderBy` (convention); all read queries use `AsNoTracking()`.

---

## 8. Deferred items / known issues (non-blocking)

Mostly content decisions and later-phase work:

1. **Home hero slides 4 & 5** link to `/aion-v` and `/aion-es`, which are hidden → **404**. Decide: hide
   those slides, or unhide/build the AION models. (Seeder + live DB both have them.)
2. **`wwwroot/pdfs/` folder is missing** — vehicle pages link `/pdfs/*-specifications.pdf` which 404 until
   the spec PDFs are added. (External "Manuals" links to `en.gacmotorsaudi.com` are fine.)
3. **News bodies + the single offer's body/image are empty** in the seed (titles/images exist). Author real
   content (or load via the Phase-6 admin).
4. **Menu labels:** top-level group is **"More"** (children Fleet Sales / Finance); the static clone used
   "Fleet Sales" as the top-level label. Owners has 5 children (incl. Road-Side Assistance) vs the static 4.
   Confirm intended labels (editable later via admin).
5. ~~`rtl.css` empty / content all EN~~ **DONE in Phase 4** — `rtl.css` filled, Cairo loaded, all DB content
   has Arabic. Remaining gap (intentional): the verbatim marketing prose in the vehicle/content/form body
   partials stays English until the Phase-6 admin makes it editable (see §5b).
6. Inert `<!-- FOOTER -->` comments remain in the 8 vehicle partials (harmless).
7. **Stale rows in the live dev DB** from earlier seeds (e.g. `.html` menu URLs, `news`/`offers` ContentPages):
   harmless because render-time `NormalizeUrl` cleans links and explicit routes win. Fresh DBs get clean data.
   Only re-seeds on an empty table (idempotent guards).

---

## 9. Next: Phase 5 — Forms & leads

Scope: make the static forms functional. The 6 `FormPage` partials (book-a-service, book-a-test-drive,
request-a-quote, contact-us, fleet, recall-enquiry) currently render STATIC (no POST). Phase 5 wires them
up: bind to view models, POST with anti-forgery, server-side validation (bilingual messages via the
`SharedResource` resx pattern from Phase 4), persist submissions to the `Lead` entity, an admin Leads inbox
view (or defer the inbox to Phase 6), and SMTP email notification. The `Lead` entity + `FormType`/`LeadStatus`
enums already exist (Phase 2). Keep the existing markup/classes (main.js + styles), just make the fields live.
