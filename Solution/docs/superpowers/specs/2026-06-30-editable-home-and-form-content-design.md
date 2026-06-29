# Editable Home Sections & Form-Page Content — Design

Date: 2026-06-30
Status: Approved (pending spec review)

## Goal

Make three currently-hardcoded areas of the public site editable from the admin
panel, **without losing any content already populated in the database** and
**without changing the lead-capture forms themselves**:

1. Home page **"Latest Offers" promo** section (`<section class="promo">`).
2. Home page **three "dual" cards** (`<section class="dual">`: Locations / Book a
   service / Parts & Accessories).
3. **Form pages** — an editable **banner image** plus the (already-stored but
   currently unrendered) **intro text**, around each lead form.

Today the promo and dual sections are static markup using compiled resource
strings (`@L["…"]`) and literal image paths; `FormPage` has no image field and
its `IntroText` is saved in the admin but never rendered for non-Contact forms.

## Data preservation (hard requirement)

The site's real database is already populated through the admin. This change
must be **non-destructive**:

- **Migration is additive only.** It creates the new tables and adds one
  **nullable** `FormPages.BannerImagePath` column. It contains **no** `DROP`,
  and **no** `ALTER`/`UPDATE` against existing columns or rows. The generated
  migration will be reviewed to confirm this before hand-off.
- **Seed/backfill is write-only-when-empty.** Every seeding step is guarded so
  it only writes where data is absent:
  - New tables (`PromoSections`, `PromoCampaigns`, `DualCards`) seed only when
    that table is empty (`if (await db.X.AnyAsync()) return;`).
  - `FormPages.BannerImagePath` is set per-slug to its current default **only
    where the column is null/empty**.
  - `FormPages.IntroText` is backfilled from the current text **only when the
    field is empty** (both `En` and `Ar` blank). Any value the user already
    entered is left untouched.
- **No table recreation / reset.** No step drops or rebuilds existing tables.
- **Render fallback.** Public views prefer the DB value and fall back to today's
  hardcoded value when a field is empty, so the site looks identical before and
  after seeding and never renders a blank section.

## Migration handling

Per decision: I generate the EF migration and seed code; the **user applies the
migration on deploy** (the app does not auto-migrate at startup).

⚠️ **Coupling:** `GetHomePageAsync` will eager-load the new tables, so the home
page (and the real-DB smoke tests) will error until the migration is applied to
that database. Deploy the migration together with this code. Verification in
this work uses in-memory-DB tests (schema built from the model, no migration
needed) so the build stays green regardless of the real DB's state.

## Data model

All new entities live in `GAC.Core/Content`, follow the existing
`LocalizedText` (owned `En`/`Ar`) convention, and hang off the **single**
`HomePage` aggregate (the same record that already owns the hero slides).

### PromoSection (one per HomePage)
```
int Id
int HomePageId            // FK → HomePage
string ImagePath          // required, max 300
LocalizedText Eyebrow     // e.g. "Promotions"
LocalizedText Heading     // "Latest Offers"
LocalizedText Description
LocalizedText CtaText      // "View Offers"
string? CtaLink            // "/offers"
List<PromoCampaign> Campaigns
```

### PromoCampaign (the bullet lines; add/remove rows)
```
int Id
int PromoSectionId        // FK → PromoSection
LocalizedText Text
int SortOrder
```

### DualCard (exactly 3, seeded; edited in place — no add/remove)
```
int Id
int HomePageId            // FK → HomePage
string ImagePath          // required, max 300
string? Link              // used for both the image link and the button
LocalizedText Eyebrow
LocalizedText Title
LocalizedText Description
LocalizedText ButtonText
int SortOrder
```

### HomePage (existing) — add
```
PromoSection? Promo
List<DualCard> DualCards
```

### FormPage (existing) — add
```
string? BannerImagePath   // plain nullable string, max 300, no owned config
```

## EF mapping & migration

- `ContentConfigurations.cs`: add `PromoSectionConfig`, `PromoCampaignConfig`,
  `DualCardConfig` mirroring `HeroSlideConfig` — `OwnsLocalized(...)` for each
  bilingual field, `ImagePath` `HasMaxLength(300).IsRequired()`, and
  `HasMany(...).WithOne().HasForeignKey(...).OnDelete(Cascade)` for the
  HomePage→Promo (one), Promo→Campaigns (many), HomePage→DualCards (many)
  relationships. Add `BannerImagePath` `HasMaxLength(300)` to `FormPageConfig`.
- Register `DbSet`s in `ApplicationDbContext` (`PromoSections`,
  `PromoCampaigns`, `DualCards`).
- `ContentService.GetHomePageAsync()`: extend the `.Include(...)` chain to load
  `Promo`, `Promo.Campaigns` (ordered), and `DualCards` (ordered).
- One new EF migration (additive — see Data preservation).

## Seeding & localization

In `ContentSeeder`, add idempotent steps that seed from the **current** resource
strings (EN from the `@L` keys, AR from `SharedResource.ar.resx`) and current
image paths, so the live site is byte-identical the moment the migration is
applied, then becomes editable:

- `SeedPromoAsync` — guard on `PromoSections` empty; attach a `PromoSection` +
  the two campaign lines to the existing HomePage.
- `SeedDualCardsAsync` — guard on `DualCards` empty; attach the 3 cards.
- `EnsureFormBannersAsync` — for each known form slug, set `BannerImagePath`
  where null and `IntroText` where blank, from current defaults.

## Admin

One new nav entry **"Home Sections"** (`_AdminNav.cshtml`, Admin/Editor roles),
served by a new `HomeSectionsController` ([Area Admin], `ContentEditor` policy,
`AutoValidateAntiforgeryToken`):

- `Index` (GET) — loads the HomePage aggregate, renders one page containing the
  promo editor and the 3 card editors.
- `SavePromo` (POST) — binds `PromoSection` incl. `Campaigns` list; upserts the
  singleton promo and replaces its campaign rows.
- `SaveCard` (POST) — binds one `DualCard` by `Id`; updates that card.

Service: extend `IAdminHomeService`/`AdminHomeService` (already owns the
HomePage aggregate and the `EnsureHomeAsync` singleton bootstrap) with
`GetHomeAsync`, `SavePromoAsync`, `SaveCardAsync`.

View: reuse `_LocalizedField` for every bilingual field, the
`data-media-input`/`data-media-pick` + `_PickerModal` image picker (with the live
preview added earlier), an add/remove rows control for promo bullets, and plain
inputs for `CtaLink`/`Link`. Each card and the promo block has its own Save.

**Form Pages** — `Areas/Admin/Views/FormPages/Edit.cshtml` gains the banner
image picker (bound to `BannerImagePath`) and continues to expose `IntroText`;
`AdminPageService.UpdateFormAsync` adds `e.BannerImagePath = page.BannerImagePath;`.
No change to slug/form-type handling or the form templates.

## Public rendering

- `Home/Index.cshtml`: replace the promo + dual markup with reads from
  `Model.Home?.Promo` and `Model.Home?.DualCards` — image, bilingual fields via
  `.Localize()`, CTA via `UrlHelpers.NormalizeUrl`, `@foreach` over campaigns and
  cards. The decorative checkmark SVG stays in the view; only text/images/links
  come from the DB. Null/empty guards keep the section rendering with current
  defaults pre-seed.
- `Views/Forms/Forms/_*.cshtml` (book-a-service, book-a-test-drive,
  request-a-quote, fleet, recall-enquiry): banner reads `BannerImagePath`
  (fallback to today's literal path), intro reads `IntroText.Localize()`
  (fallback to today's `@L` string). The `<form>…</form>` block — fields, option
  arrays, branch lists, submit — is left **byte-for-byte unchanged**.
- Contact (`Views/Forms/Page.cshtml`): optionally render the banner above the
  existing `BodyHtml` when `BannerImagePath` is set.

## Testing

- Service unit tests (in-memory DB): save/replace promo + campaigns, save a card,
  persist a form banner; verify write-only-when-empty backfill never overwrites a
  non-empty value.
- In-memory-backed integration test (same pattern as `AdminSaveRedirectTests`):
  seed a promo + cards and assert the home page renders them; assert a form page
  renders its banner + intro.
- `Slug`-style pure tests where logic warrants.
- All verification uses in-memory DB so it passes without the real-DB migration.

## Build order

- **Phase A — Home (promo + dual cards):** model → EF config + migration → seed →
  public render → admin editor → tests.
- **Phase B — Form pages (banner + intro):** model field → EF + (same) migration →
  backfill seed → admin picker → template rendering → tests.
- **Phase C:** review pass + verification.

(The migration can cover both phases in one additive migration.)

## Out of scope

- The lead forms' fields, option/branch lists, and submit behavior.
- Slug/FormType editing, dynamic add/remove of dual cards (fixed 3 by decision).
- Rich-text body for non-Contact form pages (banner + intro only, by decision).
- Reworking the hero-slides module (unchanged).
