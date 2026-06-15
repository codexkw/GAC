# GAC Motors Bilingual CMS — Handoff

**Last updated:** 2026-06-15 (end of Phase 6b)
**Repo:** https://github.com/codexkw/GAC.git (PUBLIC) · branch `main`
**Stack:** ASP.NET Core 9 MVC, EF Core 9.0.6 (SQL Server), Razor + ViewComponents, IHtmlLocalizer + .resx, xUnit.

---

## 1. What this is

A custom bilingual (English + Arabic, RTL) CMS that replaces the original static HTML pixel-clone of
`en/ar.gacmotorsaudi.com`. Frontend renders server-side from the database; an admin panel (later phase)
will manage all content. Built in **8 phases**; **Phases 1–5, 6a and 6b are complete and pushed** (admin panel live, including editable HTML page bodies). Phase 6 (admin) is now complete; **Phase 7 (Polish/QA/SEO)** is next.

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
dotnet test  Solution/GAC.sln        # 171 tests; integration tests need DB reachable
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
- **Phase 4 — Arabic / RTL** ✅. 67 tests.
- **Phase 5 — Forms & leads** ✅. 83 tests.
- **Phase 6a — Admin area (foundation + CRUD)** ✅. 163 tests.
- **Phase 6b — Admin area (editable HTML bodies)** ✅ (this handoff). `AddBodyHtml` migration applied to the GAC DB. 171 tests.
- **Phase 7 — Polish/QA/SEO** ⏭️ NEXT · **Phase 8 — Deploy** — pending.

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

## 5c. Phase 5 — Forms & leads — what was built

Plan: `docs/superpowers/plans/2026-06-15-phase5-forms-leads.md`. The 5 lead-capture forms are now functional (POST + anti-forgery + bilingual validation + DB persistence + best-effort SMTP); verified end-to-end live (valid submit → `Lead` row → 302 PRG → success banner) and via 83 green tests.

- **Which forms:** `book-a-service`, `book-a-test-drive`, `request-a-quote`, `fleet`, `recall-enquiry`. **`contact-us` has NO form** (it is a "Locate Us" directory) — only its view model line changed.
- **Flow:** each `<form>` POSTs to `FormsController.Submit` (`[HttpPost("/forms/{slug}")]`, `[ValidateAntiForgeryToken]`). Valid → map to `Lead`, persist via `ILeadService`, fire `IEmailSender` (best-effort), then **Post-Redirect-Get** to `/{slug}` where a `TempData["FormSubmitted"]` flag renders a localized success banner. Invalid → re-render the form with errors + preserved input.
- **Binding:** one `LeadFormInput` VM (`[Bind(Prefix="")]`, so the existing camelCase field names bind to PascalCase props; ModelState keys are the prop names). Container model is `FormPageViewModel { Page, Input }`. The 5 universally-required fields (Title/FirstName/LastName/Email/Phone) use DataAnnotations; **Model** is conditionally required for ServiceBooking/TestDrive/Quote and **Branch** for ServiceBooking/Fleet (checked in the controller).
- **Bilingual validation:** `.AddDataAnnotationsLocalization` over `SharedResource`; ErrorMessage strings ARE the English resx keys (same pattern as Phase 4), Arabic in `SharedResource.ar.resx`. The existing `.err` spans are now `@L[...]` and the `.field` wrapper gets an `error` class via `Html.FieldErrorClass("Prop")` when ModelState is invalid.
- **Lead mapping (`BuildLeadAsync`):** Name = Title+First+Last; VehicleId resolved by case-insensitive match of the model dropdown text against visible vehicle `Name.En`/`Name.Ar`; unmatched model text + Mileage + Marketing opt-in are folded into `Message` (the `Lead` schema is lean); DueDate `dd/MM/yyyy` → `PreferredDate`; `SourcePage = "/"+slug`; `CreatedAt = UtcNow` (also stamped defensively in `LeadService`).
- **SMTP:** `SmtpEmailSender` (MailKit **4.16.0** — pinned to dodge advisory NU1902 on the STARTTLS path) sends a plain-text notification to `Smtp:AdminNotifyEmail` (falls back to `FromEmail`), ReplyTo = lead email. It is **best-effort** — never throws to the caller (guarded + try/catch in both the sender and the controller), so an SMTP outage never breaks a submission.
- **JS:** `main.js`'s `[data-form]` handler keeps client-side validation but its success branch is now `form.submit()` (a real native submit) instead of faking a banner — so it also works with JS disabled. No other JS changed; ids/classes/`data-*`/markup preserved.
- **No new migration** — the `Leads` table already existed from Phase 2.

**SMTP secret handling (repo is PUBLIC):** committed `appsettings.json` has the `Smtp` section with `Enabled:false` and `Username`/`Password` = `__SET_LOCALLY__` (addresses are non-secret and committed). Real Mailgun creds live ONLY in gitignored `appsettings.Development.json` (`Enabled:true` + real Username/Password). **USER ACTIONS before go-live:** on the server set real `Smtp:Username`/`Smtp:Password` + `Smtp:Enabled=true`; confirm `Smtp:AdminNotifyEmail` is the real sales inbox (currently `postmaster@mg.codexkw.co`); ensure Mailgun domain `mg.codexkw.co` SPF/DKIM are valid so notifications aren't spam-filtered.

---

## 5d. Phase 6a — Admin area (foundation + CRUD) — what was built

Spec: `docs/superpowers/specs/2026-06-15-phase6-admin-design.md`. Plan: `docs/superpowers/plans/2026-06-15-phase6a-admin-foundation-crud.md`.
A new auth-gated **`GAC.Web/Areas/Admin`** (route prefix `/admin`, conventional area route `{area:exists}/{controller=Dashboard}/{action=Index}/{id?}`) with its own `_AdminLayout` (NOT the public chrome). 163 tests green; built via subagent-driven development (each task spec- + quality-reviewed). **No schema change** — all entities already existed.

- **Auth & roles:** `ConfigureApplicationCookie` (`LoginPath=/admin/login`, `AccessDeniedPath=/admin/denied`, `LogoutPath=/admin/logout`). Three authorization policies (`AdminPolicies.cs`): **`ContentEditor`** = Admin∪Editor, **`LeadsAccess`** = Admin∪Sales, **`AdminOnly`** = Admin. **Admin is a superset** (in every policy). `AccountController` = login/logout/denied (login `ReturnUrl` guarded by `Url.IsLocalUrl`). Seeded admin `admin@gacsaudi.local` / `ChangeMe!2026`.
- **Admin write-services** (NEW; separate from the read-only public services, which are untouched): `IAdminLeadService`, `IAdminVehicleService`, `IAdminMenuService`, `IAdminHomeService`, `IAdminNewsService`, `IAdminOfferService`, `IAdminPageService`, `IAdminSettingsService` (Core ifaces + Infra impls, all `AddScoped`), plus `IMediaService`. Users use `UserManager`/`RoleManager` directly (no service). Admin services use **tracked** EF + `SaveChangesAsync`; lists include hidden/unpublished rows.
- **What each section manages:** Leads (list/filter by FormType/Status/date, detail, status New→Contacted→Closed, delete) · Vehicles (meta + bilingual fields + visibility + SortOrder move + category flags + images add/remove/reorder → **drives `/models` + the mega-menu**) · Menu (tree CRUD + reorder + parent → header nav) · Hero slides · News · Offers · Content/Form page metadata (Title/Intro/Meta/IsVisible — **edit-only**; bodies come in 6b) · Site settings (Admin only) · Users (Admin only — create/edit, single role, disable via lockout, password reset; **guards against self-disable and removing the last Admin**).
- **Shared UX:** `_LocalizedField` partial renders En+Ar inputs per `LocalizedText` (field names `Foo.En`/`Foo.Ar` bind the owned type). **Media library + reusable picker** (`MediaService` → uploads to `Media:Root` config, default `wwwroot/uploads`, served by `UseStaticFiles`; image-only allow-list by extension AND content-type, GUID filenames, 5 MB cap; `X-Content-Type-Options: nosniff` now set on all static files). The picker modal (`_PickerModal`) is included on the 4 image-bearing edit views (Vehicles/HomeContent/News/Offers); `admin.js` wires `[data-media-pick]`→`[data-media-input]`.
- **Gotchas confirmed this phase:** (1) **Razor compiles at BUILD time** here — a malformed `.cshtml` fails `dotnet build` (the earlier "runtime Razor" note was wrong). (2) The `/{slug}` catch-all in `PageController` now carries a regex constraint `^(?!(?i:admin)$).*$` so the bare `/Admin` root isn't swallowed; multi-segment `/Admin/*` were always fine. (3) Tests authenticate via a `TestAuthHandler` (`X-Test-Role` header) wired in `AdminWebApplicationFactory` — it overrides only the default *authenticate* scheme, leaving the Identity cookie as the *challenge* scheme so anonymous → 302 `/admin/login`.

---

## 5e. Phase 6b — Admin area (editable HTML bodies) — what was built

Spec: `docs/superpowers/specs/2026-06-15-phase6-admin-design.md` §6. The previously-hardcoded detail/prose pages are now DB-driven and admin-editable as raw HTML; nothing changed visually (bodies seeded verbatim from the old partials). 171 tests green.

- **Schema:** owned `LocalizedText BodyHtml` (`BodyHtml_En`/`BodyHtml_Ar`) added to **`Vehicle`**, **`ContentPage`**, and **`FormPage`** (for the contact-us "Locate Us" directory). Migration **`20260615104433_AddBodyHtml`** — the FIRST Phase-6 migration — **generated AND applied to the GAC DB** (apps do not auto-migrate).
- **Seed bodies:** the markup from the 9 vehicle + 6 content + 1 contact-us partials is now embedded under `GAC.Infrastructure/SeedContent/{vehicles,content,forms}/<slug>.html` (embedded resources). `ContentSeeder.EnsureBodiesAsync` (runs at startup) **idempotently backfills `BodyHtml.En` only when blank**, matched by slug. Arabic bodies start blank → English fallback at render.
- **Generic templates:** `Views/Vehicles/Detail.cshtml`, `Views/Content/Page.cshtml`, and `Views/Forms/Page.cshtml` now render `@Html.Raw(Model.BodyHtml.Localize())`. The 16 per-slug body partials (`Views/{Vehicles/Models,Content/Pages,Forms/Forms}/_*.cshtml`) were **deleted**; the markup (custom classes, `mp-*`, sliders, tabs, `data-*`) is preserved inside the body so `main.js` keeps working. `Forms/Page.cshtml` **branches on `FormType.Contact`** (renders the body only; all other form types still render the body **plus** their functional lead form). The **5 functional lead-form partials** (book-a-service, book-a-test-drive, request-a-quote, fleet, recall-enquiry) **remain**.
- **Admin editing:** a raw-HTML body field (`_LocalizedField` with `Code = true`, En/Ar) was added to the **Vehicles**, **ContentPages**, and **FormPages** editors (FormPages only for `FormType.Contact`); the corresponding admin services now persist `BodyHtml`. `BodyHtml` is `@Html.Raw`-rendered → **trusted, admin-only** content (never user input).
- **Accepted tradeoff:** the `BodyHtml` holds the **whole page** including its hero/title markup. So the structured `Name`/`Images` fields still drive `/models` + the mega-menu (and remain editable there), but **editing a detail page's hero image or H1 title means editing the HTML body** (those are duplicated in the body markup). Arabic bodies are blank → English fallback until an editor translates them. The structured vehicle children (`SpecGroups`/`Trims`/`Colors`/`FeatureSection`) remain intentionally unmanaged (superseded by the body).

---

## 6. Routing map

| URL | Handler | Source |
|---|---|---|
| `/` | `HomeController.Index` | `Views/Home/Index.cshtml` |
| `/models` | `VehiclesController.Index` | `Views/Vehicles/Index.cshtml` |
| `/news`, `/news/{slug}` | `NewsController` | `Views/News/Index.cshtml`, `Detail.cshtml` |
| `/offers` | `OffersController.Index` | `Views/Offers/Index.cshtml` |
| `/{slug}` (catch-all) | `PageController.Show` → ContentPage \| FormPage \| visible Vehicle \| 404 | generic view → `@Html.Raw(BodyHtml)` (Phase 6b; functional form pages still dispatch to `_{slug}` partial) |
| `/not-found` | `HomeController.NotFoundPage` | `Views/Shared/NotFound.cshtml` |
| `*.html` | `LegacyHtmlRedirectMiddleware` | 301 → clean URL |
| `POST /forms/{slug}` | `FormsController.Submit` | persists `Lead`, PRG → `/{slug}` (Phase 5) |
| `/Culture/Set` | `CultureController.Set` | sets language cookie |
| `/admin/login`, `/admin/logout`, `/admin/denied` | `Areas/Admin/AccountController` | attribute-routed (Phase 6a) |
| `/Admin`, `/Admin/{controller}/{action}/{id?}` | `Areas/Admin/*Controller` | conventional area route; auth-gated by policy (Phase 6a) |

Precedence: literal attribute routes (`/models`, `/news`, `/offers`, `/not-found`) and the conventional
route (`/`, `/Culture/Set`) beat the single-segment `/{slug}`. Hidden vehicles (`aion-v`, `aion-es`,
`IsVisible=false`) return 404 (service filters them out before rendering — no 500; they also have no seed body).

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
   has Arabic. The verbatim marketing prose now lives in the editable `BodyHtml` (Phase 6b); its Arabic is
   blank → English fallback until an editor translates it (see §5e + deferred #15).
6. Inert `<!-- FOOTER -->` comments remain in the 8 vehicle partials (harmless).
8. **Rejected/forged POSTs surface as `405 Method Not Allowed` (not 400).** When a form POST fails anti-forgery (or any 4xx), `UseStatusCodePagesWithReExecute("/not-found")` re-executes the original POST against the GET-only `/not-found` page → 405. Security/function are unaffected (valid token → processed; missing/invalid token → rejected, no lead). Only the error page shown for a forged POST is affected — a path normal users never hit. Tidy later if desired (e.g. dedicated antiforgery error handling).
9. **One real "E2E Verify" lead row** (`Email='e2e.verify@example.com'`, FormType TestDrive) was inserted into the live DB during Phase-5 live verification and NOT deleted (deleting from the shared prod DB wasn't authorized). Remove it via the Phase-6 admin or a manual `DELETE FROM Leads WHERE Email='e2e.verify@example.com'`.
10. **`book-a-service` model dropdown lists `traveler` (lowercase)** while test-drive/quote list `Traveler` — faithful to the original ported markup; the controller's vehicle match is case-insensitive so it's purely cosmetic. Normalize when content becomes admin-editable.
7. **Stale rows in the live dev DB** from earlier seeds (e.g. `.html` menu URLs, `news`/`offers` ContentPages):
   harmless because render-time `NormalizeUrl` cleans links and explicit routes win. Fresh DBs get clean data.
   Only re-seeds on an empty table (idempotent guards).
11. **Admin migration not yet applied to prod / no admin deploy.** Phase 6a added NO migration (all entities
    pre-existed), so no `dotnet ef database update` is needed for it. But the admin code is committed, not deployed.
    Change the seeded admin password (`ChangeMe!2026`) before exposing `/admin` publicly.
12. **`NormalizeUrl` accepts `javascript:`-scheme URLs** (menu `Url`, hero `CtaLink`, etc.) — admin-authored,
    Razor attribute-encodes them, but a `javascript:`/`data:` scheme survives encoding in an `href`. Admin-only
    input, low risk; cheap defense-in-depth = add a scheme allow-list (`/`,`http:`,`https:`,`tel:`,`mailto:`,`#`)
    in `UrlHelpers.NormalizeUrl` (benefits all public rendering). Deferred.
13. **News `Detail.cshtml` renders `Body` via `@Html.Raw`** — ContentEditors authoring news bodies is now a
    deliberate **trusted-HTML** decision (aligns with Phase 6b's editable-HTML-body direction). If untrusted
    editors are ever added, sanitize on render.
14. **Minor admin hardening (non-blocking):** `AdminMenuService.UpdateAsync` doesn't re-validate `ParentId`
    against self/descendant on a hand-crafted POST (UI prevents it; 2-level menu); `Admin/Media/List` is unpaged;
    `DashboardController` lacks `[AutoValidateAntiforgeryToken]` (harmless — it has no POST).
15. **Arabic page bodies untranslated (Phase 6b).** The `BodyHtml.Ar` of every vehicle/content/contact-us page
    is blank → English fallback renders. Translate them via the admin raw-HTML editor (En/Ar code field) when an
    editor is available. Content-only; not a code task.
16. **`FormsController.Submit` accepts POSTs for `FormType.Contact` slugs (low risk, pre-existing).** contact-us
    has no form — today only the **view** omits it; a hand-crafted POST to `/forms/contact-us` would still create a
    `Contact` lead. Optional hardening: reject/ignore `FormType.Contact` server-side in `Submit`.
17. **Optional raw-HTML editor niceties.** The Phase-6b body editor is a plain `Code = true` textarea. Optional
    client-side polish (syntax highlighting, validation) could be added; purely cosmetic.

---

## 9. Next: Phase 7 — Polish / QA / SEO

**Phase 6 (admin) is now complete** — 6a delivered the foundation + CRUD over everything already DB-driven, and
6b (see §5e) made the previously-hardcoded detail/prose pages admin-editable as raw HTML. All admin content
management is in place; `AddBodyHtml` is applied to the GAC DB.

Remaining roadmap: **Phase 7 — Polish/QA/SEO** · **Phase 8 — Deploy**. Before go-live also clear the deferred
items in §8 — especially #1 (hidden AION hero slides), #9 (the `e2e.verify@example.com` lead — removable now via
the Leads admin), #11 (change the admin password + deploy), and #15 (translate the blank Arabic page bodies via
the new admin raw-HTML editor).
