# GAC Motors CMS — Phase 6: Admin Area — Design

**Date:** 2026-06-15
**Status:** Approved (design); pending implementation plan
**Repo:** https://github.com/codexkw/GAC.git (PUBLIC) · branch `main`
**Builds on:** Phases 1–5 (foundation, content model, public rendering, Arabic/RTL, forms & leads).
See `docs/HANDOFF.md` and `2026-06-14-gac-cms-bilingual-design.md`.

---

## 1. Goal

Build the admin panel so the dealership can manage **everything** on the site without touching
code or the database: leads, vehicles (incl. their detail-page content), the menu and mega-menu,
hero slides, news, offers, content pages, form pages, site settings, media, and admin users.

Delivered in **two sub-phases**:

- **Phase 6a — Admin foundation + CRUD** over content that is already DB-driven (no schema change).
- **Phase 6b — Editable HTML bodies** — make the currently-hardcoded detail/prose pages
  (9 vehicle, 6 content, 1 contact-us directory) DB-managed via an editable raw-HTML body field.

---

## 2. Locked decisions (this phase)

| Area | Decision |
|---|---|
| Detail-page management | **Editable HTML body per vehicle** (and per content page / contact-us) — not full structured re-modeling. The body holds the page's marketing markup; rendered with `@Html.Raw`. |
| Content breadth | **All prose pages** become editable: 9 vehicle detail bodies + 6 content pages + the contact-us "Locate Us" directory. The 5 lead forms keep their functional form markup but get editable **Title + Intro**. |
| Editor UX | **Raw-HTML code editor** (monospace textarea, optionally syntax-highlighted) — preserves the custom CSS classes (`mp-hero`, `mp-section`, `mp-tabs`, sliders, `data-*`) that a WYSIWYG would mangle. |
| Roles | **Admin = everything** (superset). **Editor = all content**. **Sales = leads only**. |
| Phasing | **Split** into 6a (foundation + CRUD) then 6b (HTML bodies). |
| Media | **Reusable media library + picker**, backed by the existing `MediaAsset` entity; files to the configurable storage root. |
| Users | **Admin manages users** — create/disable accounts, assign roles, reset passwords. |
| Publishing | Edits go live immediately (no draft/versioning) — unchanged standing decision. |
| Structured vehicle children | `Trims`, `SpecGroups`/`SpecRows`, `ColorOption`, `FeatureSection` stay in the model but remain **unused/unmanaged** — superseded by the HTML body. No CRUD built for them (YAGNI). |

---

## 3. Architecture & boundaries

- New **`GAC.Web/Areas/Admin`** (route prefix `/admin`) with `[Area("Admin")]` controllers and a
  dedicated **`_AdminLayout.cshtml`** (plain admin styling — *not* the public GAC chrome / `main.js`).
- **Write services** live in `GAC.Core/Services` (interfaces) + `GAC.Infrastructure/Services`
  (impls), **separate** from the existing read-only public services so the public read contracts
  stay clean and unchanged. Admin services use **tracked** EF queries + `SaveChangesAsync`.
- The **public site is untouched** in 6a (renders exactly as today). In 6b the only public change is
  three generic templates replacing per-slug partials, plus the additive `BodyHtml` columns.
- Edits are immediately live (no cache, no draft). All admin POSTs use **anti-forgery tokens** and
  show a TempData flash message; deletes require confirmation.

### Admin services (grouped to avoid over-fragmentation)

| Interface | Methods (shape) |
|---|---|
| `IAdminVehicleService` | list (incl. hidden), get, create, update, delete, move up/down, image add/remove/reorder/set-kind |
| `IAdminMenuService` | list tree, get, create, update, delete, move up/down, set parent |
| `IAdminHomeService` | get home, hero slide add/update/delete/reorder |
| `IAdminNewsService` | list, get, create, update, delete, toggle publish |
| `IAdminOfferService` | list, get, create, update, delete, toggle active |
| `IAdminPageService` | content pages: list/get/update (title/meta; body in 6b) · form pages: list/get/update (title/intro/meta; body in 6b) |
| `IAdminSettingsService` | get/update site settings |
| `IAdminLeadService` | list+filter (FormType/Status/date), get, set status, delete |
| `IMediaService` | upload (to storage root, create `MediaAsset`), list, delete |
| `IUserAdminService` | list, create (with role), set roles, reset password, enable/disable |

(Lead listing/status may instead extend the existing `ILeadService`; either is acceptable as long
as the public submission path keeps `CreateAsync`.)

---

## 4. Auth & roles

Identity is already configured (Phase 1) with roles **Admin / Editor / Sales** seeded and a default
admin (`admin@gacsaudi.local` / `ChangeMe!2026`).

- `ConfigureApplicationCookie`: `LoginPath = /admin/login`, `AccessDeniedPath = /admin/denied`.
- **Authorization policies** (registered in `Program.cs`):
  - `ContentEditor` → role **Admin OR Editor**
  - `LeadsAccess` → role **Admin OR Sales**
  - `AdminOnly` → role **Admin**
- **Admin is a strict superset**: it appears in every policy, so no admin action is ever gated away
  from Admin. Editor = content only; Sales = leads only.
- `AccountController` (Admin area): `GET/POST Login`, `POST Logout`, `GET Denied`. Login is the only
  anonymous admin page; all other admin controllers carry `[Authorize(Policy = …)]`.

---

## 5. Phase 6a — Admin foundation + CRUD

Controllers under `Areas/Admin/Controllers`. Every `LocalizedText` field is edited with **two inputs
(English + Arabic)** rendered by a shared `_LocalizedField` editor partial (or tag helper).

| Controller | Policy | Manages | Drives on public site |
|---|---|---|---|
| `DashboardController` | any admin | landing + counts (new leads, vehicles, pages) | — |
| `AccountController` | anon | login / logout / denied | admin gate |
| `LeadsController` | LeadsAccess | list+filter, detail, change status (New→Contacted→Closed), delete | (Phase-5 leads) |
| `VehiclesController` | ContentEditor | create/edit/delete, visibility, SortOrder (up/down), category flags, price, name/tagline/intro, **images** (add/remove/reorder, Hero vs Gallery) | `/models` listing **+ mega-menu** |
| `MenuController` | ContentEditor | menu tree CRUD, reorder, set parent | header nav |
| `HomeContentController` | ContentEditor | hero slides CRUD + reorder | home hero slider |
| `NewsController` | ContentEditor | news CRUD + publish toggle | `/news` |
| `OffersController` | ContentEditor | offers CRUD + active toggle | `/offers` |
| `ContentPagesController` | ContentEditor | title / meta (body in 6b) | content pages |
| `FormPagesController` | ContentEditor | title / intro / meta | form pages |
| `SettingsController` | AdminOnly | phone, WhatsApp, email, socials, footer tagline | header/footer chrome |
| `MediaController` | ContentEditor | upload + library + **picker** | image fields everywhere |
| `UsersController` | AdminOnly | create/disable accounts, assign roles, reset password | admin access |

**Mega-menu requirement is satisfied here:** the mega-menu is already 100% DB-driven from visible
vehicles, so managing vehicle **visibility / SortOrder / category / images** *is* managing the
mega-menu; the top nav is managed via `MenuController`.

**Media:** uploads are validated (content-type allow-list + max size), stored under the configurable
storage root with safe/unique filenames, and tracked via `MediaAsset`. A reusable modal **picker**
(list `MediaAsset` thumbnails + inline upload) backs every image field — image fields are a path
input + "Choose…" button populated by the picker.

**Reordering:** simple **move up / move down** actions adjusting `SortOrder` (deterministic,
easily testable) — not drag-and-drop.

---

## 6. Phase 6b — Editable HTML bodies

1. **Migration `AddBodyHtml`** — add `BodyHtml` (`LocalizedText` → `BodyHtml_En` / `BodyHtml_Ar`)
   to **`Vehicle`**, **`ContentPage`**, and **`FormPage`** (for the contact-us "Locate Us" directory).
2. **One-time content migration** — `ContentSeeder.EnsureBodiesAsync` (idempotent; sets `_En` only
   when blank): transcribe the existing markup from the 9 vehicle + 6 content + contact-us partials
   into the DB so **nothing changes visually**. Arabic bodies start **blank → English fallback**
   (consistent with the Phase-4 "prose stays EN until admin translates" decision).
3. **Generic templates** — `Views/Vehicles/Detail.cshtml`, `Views/Content/Page.cshtml`, and the
   contact-us branch of `Views/Forms/Page.cshtml` render `@Html.Raw(Model.BodyHtml.Localize())`.
   The 9 + 6 + 1 per-slug partials are **deleted**. Because the exact markup is pasted (custom
   classes, `mp-*`, sliders, tabs, `data-*` hooks), `main.js` keeps working unchanged.
4. **Admin editing** — add a **raw-HTML code-editor** field (En / Ar) to the Vehicles,
   ContentPages, and FormPages editors.

**Tradeoff (accepted):** the body holds the *whole* page including its hero/title markup, so editing
those means editing the HTML. The structured `Name` / `Images` fields still drive `/models` and the
mega-menu. This "body-is-the-page" reading keeps fidelity and is the simplest faithful migration.

**Security note:** `BodyHtml` is rendered with `@Html.Raw` and is therefore **trusted, admin-only
content**. It is authored exclusively by authenticated Admin/Editor users; it is never user input.

---

## 7. Testing

- **Unit (admin services):** CRUD round-trips, slug uniqueness, image add/remove/reorder/set-kind,
  lead status transition, user role assignment, media upload validation (type/size/filename).
- **Integration (`DevWebApplicationFactory`):** anonymous → 302 to `/admin/login`; wrong role →
  denied; authorized happy paths (create/edit/delete round-trip visible to the public reader).
  Role-based tests use a lightweight **test authentication handler** (authenticates as a
  header-specified role) registered in the test factory.
- **6b:** assert a seeded `BodyHtml` renders through the generic template and that the old per-slug
  partials no longer exist.

---

## 8. Delivery

- This spec covers **6a + 6b**.
- A **Phase 6a plan** is written and executed first (subagent-driven development), committed/pushed
  to public `main` like prior phases.
- The **Phase 6b plan** is written after 6a lands (matching the per-phase rhythm).

---

## 9. Secrets / public-repo discipline (unchanged, still binding)

Repo is **PUBLIC**. Real DB + SMTP creds live ONLY in gitignored `appsettings.Development.json`;
committed `appsettings.json` keeps `__SET_LOCALLY__` placeholders. Scoped `git add` only — never
`git add -A`. Scan all staged files (including docs) for secrets before commit. The default admin
password is a placeholder to be changed before go-live.
