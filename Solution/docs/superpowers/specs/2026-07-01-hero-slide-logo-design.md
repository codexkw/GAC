# Hero Slide Logo Image — Design

**Date:** 2026-07-01
**Repo:** codexkw/GAC · Solution at `C:\Users\anas-\source\repos\GAC\Solution`
**Status:** Approved (design), pending implementation plan

## Goal

Let each home-page hero slide display an uploadable **logo image** in place of the text
`<h1 class="hero__title">` (e.g. the "GS8" title), managed per slide in the admin. When a
slide has no logo, it falls back to the existing text heading.

## Background — current state

`HeroSlide` (child of `HomePage`) has: `ImagePath` (background image), `Heading`
(`LocalizedText`, the `<h1>` text), `Subheading` (eyebrow above the title), `CtaText`,
`CtaLink`, `SortOrder`. The public hero (`Views/Home/Index.cshtml`) renders per slide:
`<div class="hero__eyebrow">@Subheading</div>` → `<h1 class="hero__title">@Heading</h1>` →
CTA button. The admin edits these fields at `/Admin` hero slides → `HomeContent/Edit.cshtml`
(background image via the shared media picker, plus the localized text fields).

## Decisions (locked with the user)

1. **Logo replaces the heading, text kept as fallback.** When a slide has a logo image,
   render the logo instead of the `<h1>`. Slides with no logo fall back to the text heading.
   The heading text becomes the logo's `alt` (accessibility/SEO).
2. **One logo per slide.** A single uploadable logo image field per slide (not two, not
   per-language).

## Architecture

Additive-only, mirrors the existing `ImagePath` media-picker pattern. No new entity — one
nullable string column on `HeroSlide`.

### Data model

`HeroSlide` gains:

| Field | Type | Notes |
|---|---|---|
| `LogoImagePath` | `string?` (≤300) | uploaded logo path; null → render the text heading |

`HeroSlideConfig`: add `b.Property(s => s.LogoImagePath).HasMaxLength(300);` (same as
`ImagePath`, but nullable — not `IsRequired`).

Migration **`AddHeroSlideLogo`**: a single additive `AddColumn<string>("LogoImagePath",
"HeroSlides", nullable: true)`. No drops/alters. Safe to apply to prod before redeploy —
currently-deployed code never reads the column.

### Frontend — `Views/Home/Index.cshtml`

Replace the single title line inside the slide loop:

```razor
@if (!string.IsNullOrWhiteSpace(slide.LogoImagePath))
{
    <img class="hero__logo" src="@slide.LogoImagePath" alt="@slide.Heading.Localize()" />
}
else
{
    <h1 class="hero__title">@slide.Heading.Localize()</h1>
}
```

Eyebrow and CTA are unchanged. The `alt` uses the localized heading so the slide still has
an accessible/indexable title.

### Styling — `wwwroot/assets/css/styles.css`

Add `.hero__logo`, sized to occupy the title's visual slot:

```css
.hero__logo {
  display: block;
  max-height: clamp(2.5rem, 5.5vw, 4.5rem);   /* matches .hero__title font-size range */
  width: auto;
  max-width: min(90%, 520px);
  margin-bottom: var(--space-5);              /* same as .hero__title */
  filter: drop-shadow(0 2px 30px rgba(0,0,0,.4));
}
```

Plus a mobile override matching the existing `.hero__title` breakpoints (≈`max-height:
clamp(2rem, 8vw, 3rem)` and the reduced margin) at the same media queries where
`.hero__title` is overridden. RTL needs no change (the logo is centered like the title).

### Admin — `Areas/Admin/Views/HomeContent/Edit.cshtml`

Add a **Logo image** field using the same media-picker markup as `ImagePath`
(`data-media-input` + `data-media-pick`, `_PickerModal` already on the page), placed right
below the Heading field, labeled *"Logo image (optional — replaces the heading text when
set)"*. Binds to `LogoImagePath`. The controller already binds the full `HeroSlide`, so no
controller change is required.

### Seeding

No seed change. `LogoImagePath` starts null on every slide, so existing slides keep showing
their text heading until the admin uploads a logo. No backfill.

## Testing (TDD)

In-memory EF (`UseInMemoryDatabase`); render tests via `WebApplicationFactory<Program>` with
`UseEnvironment("Development")` + `InMemoryTestDb.Swap`.

- **`HeroSlideLogoMappingTests`** (`GAC.Tests/Content/`) — a `HeroSlide` with `LogoImagePath`
  round-trips through save/reload.
- **`HeroLogoRenderTests`** (`GAC.Tests/Home/`) — set one seeded slide's `LogoImagePath` and
  leave another null; assert the home page emits `<img class="hero__logo"` with that src for
  the first and still emits `<h1 class="hero__title"` for the second.
- **Admin edit-view check** — the `HomeContent/Edit` form contains an input bound to
  `LogoImagePath` (rendered markup contains `name="LogoImagePath"`). Run any admin in-memory
  test class by **explicit class name** (the `GAC.Tests.Admin` namespace contains prod-DB
  classes).

## Global constraints

- .NET 9 / EF Core 9; SQL Server (prod), EF InMemory (tests); pin `Microsoft.*` to `9.0.*`.
- Additive migration only; apply to prod via the scoped idempotent script, then the reliable
  .NET `SqlConnection` apply method (sqlcmd.exe is flaky on this box). Never
  `dotnet ef database update`.
- No secrets in committed files.

## Out of scope (YAGNI)

- A second logo per slide, per-language logos.
- Logos for any component other than hero slides.
- Removing or changing the `Heading` field (kept as fallback + alt).
