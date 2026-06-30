# Editable Offers + Road-Assistance — Design Spec

**Date:** 2026-06-30
**Repo:** codexkw/GAC · solution at `C:\Users\anas-\source\repos\GAC\Solution`
**Branch:** `feature/editable-offers-road-assistance` (off `main`)

## Goal

Make two public pages truly admin-managed and bilingual:

1. **`/offers`** — wire the (already hardcoded) page to the existing admin Offers data, add a per-offer **Button name** field, and localize the static page chrome to Arabic.
2. **`/road-assistance`** — convert the raw-HTML-blob Content Page into a **structured admin editor** (heading, intro, contact lead, contact text, phone number, call-button label), mirroring the Warranty page.

## Current state (verified)

- **Offers:** `OffersController.Index` already calls `GetActiveOffersAsync()` (→ `Offers` where `IsActive`, ordered by `SortOrder`) and passes the list to `Views/Offers/Index.cshtml`. **But that view ignores the model entirely** — hero, 6 offer cards, and the bottom CTA are hardcoded English HTML. A full admin Offers CRUD already exists (`Areas/Admin/Controllers/OffersController`, `IAdminOfferService`, `Offer` entity: `Slug, IsActive, Title, Body, ImagePath, ValidUntil, SortOrder`). `SeedOffersAsync` seeds **one** empty placeholder offer (`current-offers`, no body).
- **Road-assistance:** a `ContentPage` (slug `road-assistance`, visible) whose `BodyHtml` is backfilled from the embedded `SeedContent/content/road-assistance.html` blob and rendered by `Views/Content/Page.cshtml` = `@Html.Raw(BodyHtml)`. Editable today only as one raw-HTML field. Content: heading, two intro paragraphs, a "Getting In Touch" bold lead, an instruction paragraph, and a `tel:1833334` "Call 1833334" button.

## Design

### Offers

**Model:** add `public LocalizedText ButtonLabel { get; set; } = new();` to `Offer`. `OfferConfig` gets `b.OwnsLocalized(o => o.ButtonLabel);` → columns `ButtonLabel_En`, `ButtonLabel_Ar`. **Migration `AddOfferButtonLabel`** (additive: 2 nullable string columns).

**Admin:** add a "Button name" `_LocalizedField` to `Areas/Admin/Views/Offers/Edit.cshtml`. `AdminOfferService.UpdateAsync` maps `e.ButtonLabel = a.ButtonLabel;`. Image/Valid-until fields stay in the form (no data wiped); they just don't render on the card. No badge.

**Public view (`Views/Offers/Index.cshtml`):** render `@Model`:
- Crumb/hero/CTA localized via `@L[...]`: `Home`, `Offers`, `Latest Offers`, hero subtitle, `Found an offer you like?`, CTA text, `Contact Sales` (existing keys reused where present).
- One `.offer-card` per offer: `Title` → `.offer-card__title`, `Body` → `.offer-card__text`, button → `.btn.btn--accent` linking **fixed** to `/request-a-quote`. Button text = `ButtonLabel.Localize()`, falling back to `L["Enquire Now"]` when blank.
- Empty state: localized "no current offers" line when the list is empty.

**Seeder (`SeedOffersAsync`):** seed the **6 real cards** (EN + AR + button label, `SortOrder` 1–6), write-only-when-empty. Because the legacy single placeholder (`current-offers`, empty body) would block the guard and render as a lone empty card on prod, first **retire it**: if the only row is `current-offers` with a blank `Body.En`, delete it, then seed. Real admin-entered offers (any other slug, or >1 row) are never touched.

**Resources (`SharedResource.ar.resx`):** add Arabic for `Offers`, the hero subtitle, `Found an offer you like?`, the CTA text, `Contact Sales`, `Enquire Now`. (`Latest Offers`, `Home`, `Back`, `Homepage`, `Owners` already exist.)

### Road-assistance

**Model:** new singleton `RoadAssistancePage` (mirrors `WarrantyPage`, no child collection):
```csharp
public class RoadAssistancePage
{
    public int Id { get; set; }
    public LocalizedText Heading { get; set; } = new();
    public LocalizedText Intro { get; set; } = new();           // multiline → paragraphs
    public LocalizedText ContactLead { get; set; } = new();     // "Getting In Touch"
    public LocalizedText ContactText { get; set; } = new();     // instruction paragraph
    public string PhoneNumber { get; set; } = "";               // drives tel: link
    public LocalizedText CallButtonLabel { get; set; } = new(); // "Call 1833334"
}
```
`RoadAssistancePageConfig`: `OwnsLocalized` the four `LocalizedText`; `Property(PhoneNumber).HasMaxLength(40)`. `DbSet<RoadAssistancePage> RoadAssistancePages`. **Migration `AddRoadAssistancePage`** (additive: one CreateTable).

**Services:** `IContentService.GetRoadAssistancePageAsync()` (`AsNoTracking().FirstOrDefault()`). `IAdminRoadAssistanceService` + `AdminRoadAssistanceService` (`GetAsync` ensure+load singleton; `SaveAsync` upsert all fields). Register in `Program.cs`.

**Seeder (`SeedRoadAssistanceAsync`, write-only-when-empty):** seed EN (from the live page) + AR. `PhoneNumber = "1833334"`, `CallButtonLabel = { En = "Call 1833334", Ar = "اتصل 1833334" }`.

**Admin:** `Areas/Admin/Controllers/RoadAssistanceController` (Index GET, Save POST → redirect `{ area = "Admin" }`), `Areas/Admin/Views/RoadAssistance/Index.cshtml` (structured `_LocalizedField`s + a plain phone input), `_AdminNav` link "Road Assistance".

**Public render:** `PageController.Show` special-cases `content.Slug == "road-assistance"` (like warranty) → load `GetRoadAssistancePageAsync()` → render `Views/Content/RoadAssistance.cshtml` (same `cos-head`/`op-btns` markup as the seed HTML; intro paragraph split on `\n`; button `href="tel:{digits}"`). `Areas/Admin/Views/ContentPages/Edit.cshtml` hides `BodyHtml` for `road-assistance` too (hidden inputs preserve it) with a note linking to the new editor.

## Constraints / non-goals

- **Additive only.** Two new migrations; new columns/table only, drops only in `Down()`. Non-breaking for currently-deployed code (it never reads `ButtonLabel` or the new table).
- Offer button link stays `/request-a-quote` (not per-offer editable). No badge, no card image, no valid-until on the card.
- Static offers chrome is localized via resources, **not** admin-managed (per the request).
- TDD with the in-memory harness; trailing-dot test namespaces only (never the prod-DB smoke classes). See `[[gac_cms_pivot]]`, `[[gac_editable_home_form_warranty_branch]]`.

## Testing

In-memory only (`InMemoryTestDb.Swap` / `AdminInMemoryWebApplicationFactory`):
- Offers: `ButtonLabel` mapping round-trip; `SeedOffersAsync` seeds 6 + retires placeholder + skips when real offers exist; offers page renders cards from model + localizes chrome (EN + AR); admin save persists `ButtonLabel`.
- Road-assistance: model/config mapping round-trip; seeder write-only-when-empty; `AdminRoadAssistanceService` upsert; admin Save redirect; public render shows heading + phone button; `PageController` routes the slug to the structured view.
