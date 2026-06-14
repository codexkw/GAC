# Phase 4 — Arabic / RTL Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the GAC CMS fully bilingual — when the language cookie is `ar`, the site renders in Arabic with a correct right-to-left layout and the Cairo webfont; English is unchanged.

**Architecture:** All DB-driven text already renders through the `LocalizedText.Localize()` extension (verified across every view), so Arabic DB content "just works" once the seeder writes Arabic values. Three pieces are missing and form this phase: (1) Arabic content in the database, (2) Arabic translations for the hardcoded static UI strings in chrome and a few views, and (3) the RTL stylesheet + Arabic font. We add a tiny resource-file (`IHtmlLocalizer`) layer for static strings, an idempotent Arabic backfill in `ContentSeeder`, and fill `rtl.css`. **No `main.js` changes** — RTL slider correctness is achieved purely with the CSS `direction: ltr` track technique.

**Tech Stack:** ASP.NET Core 9 MVC, EF Core 9.0.6, Razor + ViewComponents, `Microsoft.Extensions.Localization` (`IHtmlLocalizer` + `.resx`), xUnit.

**Scope (locked with user 2026-06-15):**
- **IN:** RTL layout site-wide; Cairo Arabic font; Arabic for ALL DB-driven content (chrome menu, vehicle names + taglines, hero slides, page titles, news/offer titles, footer tagline); Arabic for static chrome/view UI strings; EN+AR visual parity pass.
- **OUT (stays English until Phase 6 admin):** the deep hardcoded marketing prose inside the ported vehicle-detail partials (`Views/Vehicles/Models/_*.cshtml`) and the content/form page body partials (`Views/Content/Pages/_*.cshtml`, `Views/Forms/Forms/_*.cshtml`). Only their DB-bound `@Model.Title.Localize()` headings become Arabic. RTL layout still applies to these pages.
- **OUT:** forms remain static (Phase 5), no admin (Phase 6), Latin numerals retained.

---

## File Structure

**Create:**
- `GAC.Web/Resources/SharedResource.cs` — empty marker class for the shared `IHtmlLocalizer`.
- `GAC.Web/Resources/SharedResource.ar.resx` — Arabic translations of static UI strings (keys are the English text; English needs no resx because the key is the fallback).
- `GAC.Tests/ArabicSeedTests.cs` — asserts the seeder/backfill writes Arabic.
- `GAC.Tests/ArabicRenderTests.cs` — integration tests that the site renders Arabic + RTL assets under the `ar` cookie.

**Modify:**
- `GAC.Web/Program.cs` — `AddLocalization` + `AddViewLocalization`.
- `GAC.Web/Views/_ViewImports.cshtml` — `@inject IHtmlLocalizer<SharedResource> L` + usings.
- `GAC.Web/Views/Shared/_Layout.cshtml` — conditional Cairo font link (RTL only).
- `GAC.Web/Views/Shared/Components/Header/Default.cshtml` — localize static strings; fix lang-switch active label.
- `GAC.Web/Views/Shared/Components/Footer/Default.cshtml` — localize static strings.
- `GAC.Web/Views/Home/Index.cshtml`, `Views/Vehicles/Index.cshtml`, `Views/News/Index.cshtml` — localize remaining static UI strings.
- `GAC.Infrastructure/Data/ContentSeeder.cs` — add `EnsureArabicAsync` idempotent backfill + Tagline seeding.
- `GAC.Web/wwwroot/assets/css/rtl.css` — the RTL overrides (currently an empty placeholder).

**Convention reminder (from Phase 3 / HANDOFF §7):** Razor `~/` does NOT resolve in CSS `url()` (use `/assets/...`); literal `@` must be `@@`; preserve every class/id/`data-*` (main.js keys off them); `<partial>` 500s on a missing view.

---

## Task 1: Localization infrastructure (resx + IHtmlLocalizer)

Sets up a shared resource so static UI strings can be translated. Keys ARE the English text, so only an Arabic resx is needed (missing key → returns the key → English).

**Files:**
- Create: `GAC.Web/Resources/SharedResource.cs`
- Create: `GAC.Web/Resources/SharedResource.ar.resx`
- Modify: `GAC.Web/Program.cs`
- Modify: `GAC.Web/Views/_ViewImports.cshtml`

- [ ] **Step 1: Create the marker class**

`GAC.Web/Resources/SharedResource.cs`:
```csharp
namespace GAC.Web.Resources;

/// <summary>
/// Marker type for the shared <see cref="Microsoft.Extensions.Localization.IHtmlLocalizer{T}"/>.
/// Translations live in SharedResource.{culture}.resx. Resource KEYS are the English source
/// text, so English needs no resx file (a missing key returns the key verbatim).
/// </summary>
public sealed class SharedResource
{
}
```

- [ ] **Step 2: Register localization in `Program.cs`**

In `GAC.Web/Program.cs`, add localization services. Change:
```csharp
builder.Services.AddControllersWithViews();
```
to:
```csharp
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services
    .AddControllersWithViews()
    .AddViewLocalization();
```
Leave the existing `RequestLocalization` config and `app.UseRequestLocalization();` untouched (already correct).

- [ ] **Step 3: Inject the localizer globally in `_ViewImports.cshtml`**

Append to `GAC.Web/Views/_ViewImports.cshtml`:
```cshtml
@using GAC.Web.Resources
@using Microsoft.AspNetCore.Mvc.Localization
@inject IHtmlLocalizer<SharedResource> L
```

- [ ] **Step 4: Create `SharedResource.ar.resx` with every static UI string**

`GAC.Web/Resources/SharedResource.ar.resx` (standard .resx header, then the data entries below). Use this EXACT file:
```xml
<?xml version="1.0" encoding="utf-8"?>
<root>
  <xsd:schema id="root" xmlns="" xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:msdata="urn:schemas-microsoft-com:xml-msdata">
    <xsd:import namespace="http://www.w3.org/XML/1998/namespace" />
    <xsd:element name="root" msdata:IsDataSet="true">
      <xsd:complexType>
        <xsd:choice maxOccurs="unbounded">
          <xsd:element name="metadata">
            <xsd:complexType>
              <xsd:sequence>
                <xsd:element name="value" type="xsd:string" minOccurs="0" />
              </xsd:sequence>
              <xsd:attribute name="name" use="required" type="xsd:string" />
              <xsd:attribute name="type" type="xsd:string" />
              <xsd:attribute name="mimetype" type="xsd:string" />
              <xsd:attribute ref="xml:space" />
            </xsd:complexType>
          </xsd:element>
          <xsd:element name="assembly">
            <xsd:complexType>
              <xsd:attribute name="alias" type="xsd:string" />
              <xsd:attribute name="name" type="xsd:string" />
            </xsd:complexType>
          </xsd:element>
          <xsd:element name="data">
            <xsd:complexType>
              <xsd:sequence>
                <xsd:element name="value" type="xsd:string" minOccurs="0" msdata:Ordinal="1" />
                <xsd:element name="comment" type="xsd:string" minOccurs="0" msdata:Ordinal="2" />
              </xsd:sequence>
              <xsd:attribute name="name" type="xsd:string" use="required" msdata:Ordinal="1" />
              <xsd:attribute name="type" type="xsd:string" msdata:Ordinal="3" />
              <xsd:attribute name="mimetype" type="xsd:string" msdata:Ordinal="4" />
              <xsd:attribute ref="xml:space" />
            </xsd:complexType>
          </xsd:element>
          <xsd:element name="resheader">
            <xsd:complexType>
              <xsd:sequence>
                <xsd:element name="value" type="xsd:string" minOccurs="0" msdata:Ordinal="1" />
              </xsd:sequence>
              <xsd:attribute name="name" type="xsd:string" use="required" />
            </xsd:complexType>
          </xsd:element>
        </xsd:choice>
      </xsd:complexType>
    </xsd:element>
  </xsd:schema>
  <resheader name="resmimetype"><value>text/microsoft-resx</value></resheader>
  <resheader name="version"><value>2.0</value></resheader>
  <resheader name="reader"><value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value></resheader>
  <resheader name="writer"><value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value></resheader>

  <data name="WhatsApp" xml:space="preserve"><value>واتساب</value></data>
  <data name="Call {0}" xml:space="preserve"><value>اتصل {0}</value></data>
  <data name="All" xml:space="preserve"><value>الكل</value></data>
  <data name="Sedan" xml:space="preserve"><value>سيدان</value></data>
  <data name="SUV" xml:space="preserve"><value>دفع رباعي</value></data>
  <data name="EV" xml:space="preserve"><value>كهربائية</value></data>
  <data name="All Models" xml:space="preserve"><value>كل الموديلات</value></data>
  <data name="English" xml:space="preserve"><value>English</value></data>

  <data name="Chat on WhatsApp" xml:space="preserve"><value>تواصل عبر واتساب</value></data>
  <data name="Book a Test Drive" xml:space="preserve"><value>احجز تجربة قيادة</value></data>
  <data name="Test Drive" xml:space="preserve"><value>تجربة قيادة</value></data>
  <data name="Get Online Quote" xml:space="preserve"><value>اطلب عرض سعر</value></data>
  <data name="Quote" xml:space="preserve"><value>عرض سعر</value></data>
  <data name="Download Brochure" xml:space="preserve"><value>حمّل الكتيب</value></data>
  <data name="Brochure" xml:space="preserve"><value>الكتيب</value></data>
  <data name="Find Showroom" xml:space="preserve"><value>أوجد المعرض</value></data>
  <data name="Showroom" xml:space="preserve"><value>المعرض</value></data>
  <data name="Contact Us" xml:space="preserve"><value>تواصل معنا</value></data>
  <data name="Contact" xml:space="preserve"><value>تواصل</value></data>

  <data name="Privacy Notice" xml:space="preserve"><value>إشعار الخصوصية</value></data>
  <data name="Site Map" xml:space="preserve"><value>خريطة الموقع</value></data>
  <data name="About Us" xml:space="preserve"><value>من نحن</value></data>
  <data name="Suggestions &amp; Complaints" xml:space="preserve"><value>الاقتراحات والشكاوى</value></data>
  <data name="Back to Top" xml:space="preserve"><value>العودة للأعلى</value></data>

  <data name="Explore" xml:space="preserve"><value>اكتشف</value></data>
  <data name="Read More" xml:space="preserve"><value>اقرأ المزيد</value></data>
  <data name="News" xml:space="preserve"><value>الأخبار</value></data>
  <data name="Latest News" xml:space="preserve"><value>أحدث الأخبار</value></data>
  <data name="Find Your GAC" xml:space="preserve"><value>اختر سيارتك من GAC</value></data>
  <data name="Search" xml:space="preserve"><value>بحث</value></data>
</root>
```

- [ ] **Step 5: Build to verify the resx compiles and DI resolves**

Run: `dotnet build Solution/GAC.sln -c Debug`
Expected: build succeeds (no resx parse error; `IHtmlLocalizer<SharedResource>` registered).

- [ ] **Step 6: Commit**

```bash
git add Solution/GAC.Web/Resources/SharedResource.cs Solution/GAC.Web/Resources/SharedResource.ar.resx Solution/GAC.Web/Program.cs Solution/GAC.Web/Views/_ViewImports.cshtml
git commit -m "feat(phase4): add shared resx localization infrastructure"
```

---

## Task 2: Localize static UI strings in chrome + views

Replace every hardcoded English UI string (NOT page body prose) with `@L["..."]`. Also fix the language switch so the visible label reflects the **current** language and marks the active option.

**Files:**
- Modify: `GAC.Web/Views/Shared/Components/Header/Default.cshtml`
- Modify: `GAC.Web/Views/Shared/Components/Footer/Default.cshtml`
- Modify: `GAC.Web/Views/Home/Index.cshtml`
- Modify: `GAC.Web/Views/Vehicles/Index.cshtml`
- Modify: `GAC.Web/Views/News/Index.cshtml`

- [ ] **Step 1: Header — megamenu tabs, drawer, WhatsApp, Call, lang label**

In `Views/Shared/Components/Header/Default.cshtml`:

1. Add at the top under the existing `@{ ... }` block a culture flag:
```cshtml
@{
    var menu = Model.Menu;
    var vehicles = Model.Vehicles;
    bool IsModels(MenuItem mi) => UrlHelpers.NormalizeUrl(mi.Url) == "/models";
    var isAr = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar";
}
```

2. Desktop WhatsApp link text (line ~19): change trailing ` WhatsApp` to ` @L["WhatsApp"]`.

3. Megamenu tabs (lines ~52-55): keep `data-mm-tab` values EXACTLY (`all`/`sedan`/`suv`/`ev` — main.js keys off them); translate only the visible label:
```cshtml
<button class="megamenu__tab is-active" data-mm-tab="all">@L["All"]</button>
<button class="megamenu__tab" data-mm-tab="sedan">@L["Sedan"]</button>
<button class="megamenu__tab" data-mm-tab="suv">@L["SUV"]</button>
<button class="megamenu__tab" data-mm-tab="ev">@L["EV"]</button>
```

4. Lang switch visible button label (line ~24) — reflect current language:
```cshtml
<span>@(isAr ? "ع" : "EN")</span>
```

5. Lang menu options — translate "English" label and mark active (line ~28-29):
```cshtml
<li><a asp-controller="Culture" asp-action="Set" asp-route-culture="en" asp-route-returnUrl="@Context.Request.Path" class="@(isAr ? "" : "is-active")">@L["English"]</a></li>
<li><a asp-controller="Culture" asp-action="Set" asp-route-culture="ar" asp-route-returnUrl="@Context.Request.Path" lang="ar" class="@(isAr ? "is-active" : "")">عربي</a></li>
```

6. Drawer "All Models" (line ~101): `<a href="/models">@L["All Models"]</a>`.

7. Drawer footer links (lines ~126-127):
```cshtml
<a href="https://api.whatsapp.com/send/?phone=@Model.Settings.WhatsApp" target="_blank" rel="noopener">@L["WhatsApp"]</a>
<a href="tel:@Model.Settings.Phone">@L["Call {0}", Model.Settings.Phone]</a>
```

- [ ] **Step 2: Footer — action-dock labels + footer nav**

In `Views/Shared/Components/Footer/Default.cshtml`, replace each action-dock `__full`/`__short` pair and each footer nav link with `@L[...]`. Keep all `class`, `href`, `title`, SVG markup unchanged. Example for the WhatsApp dock item (line ~8):
```cshtml
<span class="action-dock__text"><span class="action-dock__full">@L["Chat on WhatsApp"]</span><span class="action-dock__short">@L["WhatsApp"]</span></span>
```
Apply the same pattern for: Book a Test Drive / Test Drive; Get Online Quote / Quote; Download Brochure / Brochure; Find Showroom / Showroom; Contact Us / Contact.

Footer nav (lines ~37-43):
```cshtml
<a title="Privacy Notice" href="/privacy-policy">@L["Privacy Notice"]</a>
<a title="Site Map" href="#">@L["Site Map"]</a>
<a title="About Us" href="/about">@L["About Us"]</a>
<a title="Contact Us" href="/contact-us">@L["Contact Us"]</a>
<a title="Suggestions &amp; Complaints" href="#">@L["Suggestions & Complaints"]</a>
...
<a class="footer-totop" href="#page-wrap">@L["Back to Top"]</a>
```
(The `title` attributes may stay English — they are not visible text; translating them is optional and out of scope.)

- [ ] **Step 3: Home / Vehicles / News static strings**

- `Views/Home/Index.cshtml`: translate the four model-tab labels (find the tab buttons that read All/Sedan/SUV/EV — translate the visible text only, keep any `data-*`/filter values), the "News"/"Latest News" section heading, the search block label/button ("Find Your GAC", "Search"). Use the matching `@L[...]` keys. Do NOT touch `@v.Name.Localize()` / `@slide.Heading.Localize()` (already localized).
- `Views/Vehicles/Index.cshtml` (line ~27): `>Explore<` → `>@L["Explore"]<`, `>Quote<` → `>@L["Quote"]<`.
- `Views/News/Index.cshtml` (line ~20): `>Read More<` → `>@L["Read More"]<`.

> If a string in these views has no matching key in the resx, add the key to `SharedResource.ar.resx` (English key + Arabic value) rather than leaving it hardcoded.

- [ ] **Step 4: Build + run a quick smoke**

Run: `dotnet build Solution/GAC.sln -c Debug`
Expected: succeeds. (Render verification happens in Task 5 tests + Task 6 visual pass.)

- [ ] **Step 5: Commit**

```bash
git add Solution/GAC.Web/Views/Shared/Components/Header/Default.cshtml Solution/GAC.Web/Views/Shared/Components/Footer/Default.cshtml Solution/GAC.Web/Views/Home/Index.cshtml Solution/GAC.Web/Views/Vehicles/Index.cshtml Solution/GAC.Web/Views/News/Index.cshtml
git commit -m "feat(phase4): localize static chrome and view UI strings"
```

---

## Task 3: Seed Arabic for all DB content (idempotent backfill)

The seeder currently writes English only, and its `if (await ...AnyAsync()) return;` guards mean it will NOT add Arabic to the existing dev DB. Add a single idempotent `EnsureArabicAsync(db)` pass that sets the `_Ar` column of each `LocalizedText` field **only when it is currently null/empty**, matched by natural key (slug / sort order / label). This works for BOTH fresh DBs and the existing dev DB, and is safe to run on every startup. Arabic strings live ONLY here (no duplication with the inserts).

**Files:**
- Modify: `GAC.Infrastructure/Data/ContentSeeder.cs`

- [ ] **Step 1: Call the backfill at the end of `SeedAsync`**

In `ContentSeeder.SeedAsync`, after `await SeedOffersAsync(db);` add:
```csharp
        await EnsureArabicAsync(db);
```

- [ ] **Step 2: Add the `EnsureArabicAsync` method**

Add this method to `ContentSeeder` (uses `Microsoft.EntityFrameworkCore`, already imported). It mutates the owned `LocalizedText` by replacing the instance so EF tracks the change, and only writes when `Ar` is blank (idempotent). `Tagline` is also populated (EN+AR) since it was never seeded.

```csharp
    // ──────────────────────────────────────────────
    //  Arabic backfill (Phase 4). Idempotent: only sets a field's Arabic
    //  when it is currently null/empty. Matches rows by natural key so it
    //  works on both fresh and previously-seeded (EN-only) databases.
    // ──────────────────────────────────────────────
    private static async Task EnsureArabicAsync(ApplicationDbContext db)
    {
        // Helper: set Ar on an owned LocalizedText only if currently blank.
        static bool SetAr(LocalizedText? field, string ar, out LocalizedText result)
        {
            var en = field?.En ?? string.Empty;
            if (field is not null && !string.IsNullOrWhiteSpace(field.Ar))
            {
                result = field;
                return false;
            }
            result = new LocalizedText { En = en, Ar = ar };
            return true;
        }

        var changed = false;

        // Site settings (FooterTagline)
        var settings = await db.SiteSettings.FirstOrDefaultAsync();
        if (settings is not null && SetAr(settings.FooterTagline, "جي إيه سي مطوع القاضي للسيارات", out var tagline))
        {
            settings.FooterTagline = tagline;
            changed = true;
        }

        // Vehicles: Name + Tagline, keyed by slug
        var vehicleAr = new Dictionary<string, (string Name, string Tagline)>
        {
            ["gs8traveller"] = ("GS8 ترافيلر", "دفع رباعي فاخر بسبعة مقاعد"),
            ["gs8"]          = ("GS8", "دفع رباعي متوسط فاخر"),
            ["gs3emzoom"]    = ("إمزوم", "دفع رباعي مدمج عصري"),
            ["emkoo"]        = ("إمكو", "دفع رباعي رياضي أنيق"),
            ["empow"]        = ("إمبو", "سيدان رياضية عالية الأداء"),
            ["m8"]           = ("M8", "ميني فان فاخرة"),
            ["empow-sport"]  = ("إمبو R", "سيدان رياضية بأداء فائق"),
            ["aion-v"]       = ("أيون V", "دفع رباعي كهربائي بالكامل"),
            ["aion-es"]      = ("أيون ES", "سيدان كهربائية بالكامل"),
            ["hyptec-ht"]    = ("هايبتك HT", "دفع رباعي كهربائي فاخر"),
            ["gs4"]          = ("GS4 ماكس", "دفع رباعي مدمج عملي"),
        };
        foreach (var v in await db.Vehicles.ToListAsync())
        {
            if (!vehicleAr.TryGetValue(v.Slug, out var ar)) continue;
            if (SetAr(v.Name, ar.Name, out var name)) { v.Name = name; changed = true; }
            if (SetAr(v.Tagline, ar.Tagline, out var tag)) { v.Tagline = tag; changed = true; }
        }

        // Hero slides: Heading, keyed by SortOrder
        var slideAr = new Dictionary<int, string>
        {
            [1] = "جي إيه سي موتور", [2] = "GS4 ماكس", [3] = "هايبتك HT",
            [4] = "أيون V", [5] = "أيون ES", [6] = "إمبو R",
            [7] = "GS8 ترافيلر", [8] = "M8", [9] = "إمزوم",
        };
        foreach (var s in await db.HeroSlides.ToListAsync())
        {
            if (!slideAr.TryGetValue(s.SortOrder, out var ar)) continue;
            if (SetAr(s.Heading, ar, out var h)) { s.Heading = h; changed = true; }
        }

        // Menu items: Label, keyed by the English label (unique enough across the seed)
        var menuAr = new Dictionary<string, string>
        {
            ["Home"] = "الرئيسية",
            ["Models"] = "الموديلات",
            ["Owners"] = "الملاك",
            ["Book a Service"] = "احجز صيانة",
            ["Cost of Service"] = "تكلفة الصيانة",
            ["Warranty"] = "الضمان",
            ["Recall"] = "استدعاء",
            ["Road-Side Assistance"] = "المساعدة على الطريق",
            ["Shopping Tools"] = "أدوات التسوق",
            ["Book a Test Drive"] = "احجز تجربة قيادة",
            ["Request a Quote"] = "اطلب عرض سعر",
            ["Locations"] = "المواقع",
            ["More"] = "المزيد",
            ["Fleet Sales"] = "مبيعات الأساطيل",
            ["Finance"] = "التمويل",
        };
        foreach (var m in await db.MenuItems.ToListAsync())
        {
            if (m.Label is null || !menuAr.TryGetValue(m.Label.En, out var ar)) continue;
            if (SetAr(m.Label, ar, out var lbl)) { m.Label = lbl; changed = true; }
        }

        // Content pages: Title, keyed by slug
        var contentAr = new Dictionary<string, string>
        {
            ["about"] = "من نحن",
            ["warranty"] = "الضمان",
            ["privacy-policy"] = "سياسة الخصوصية",
            ["finance"] = "تمويل تيسير",
            ["cost-of-service"] = "تكلفة الصيانة",
            ["road-assistance"] = "المساعدة على الطريق",
        };
        foreach (var p in await db.ContentPages.ToListAsync())
        {
            if (!contentAr.TryGetValue(p.Slug, out var ar)) continue;
            if (SetAr(p.Title, ar, out var t)) { p.Title = t; changed = true; }
        }

        // Form pages: Title, keyed by slug
        var formAr = new Dictionary<string, string>
        {
            ["book-a-service"] = "احجز صيانة",
            ["book-a-test-drive"] = "احجز تجربة قيادة",
            ["request-a-quote"] = "اطلب عرض سعر",
            ["contact-us"] = "أوجدنا",
            ["fleet"] = "الأساطيل",
            ["recall-enquiry"] = "استعلام استدعاء",
        };
        foreach (var f in await db.FormPages.ToListAsync())
        {
            if (!formAr.TryGetValue(f.Slug, out var ar)) continue;
            if (SetAr(f.Title, ar, out var t)) { f.Title = t; changed = true; }
        }

        // News articles: Title, keyed by slug
        var newsAr = new Dictionary<string, string>
        {
            ["gac-empow-2026-high-performance-sports-sedan"] =
                "جي إيه سي إمبو 2026: السيدان الرياضية عالية الأداء بمحرك جديد",
            ["mutawa-alkadi-intensive-training-technical-competition"] =
                "مطوع القاضي للسيارات تنظّم تدريباً مكثفاً ومسابقة فنية لفنيي جي إيه سي موتور",
            ["emzoom-first-chinese-car-quality-ranking-2024"] =
                "مطوع القاضي للسيارات تعلن تصدّر إمزوم تصنيف جودة السيارات الصينية للنصف الأول من 2024",
        };
        foreach (var n in await db.NewsArticles.ToListAsync())
        {
            if (!newsAr.TryGetValue(n.Slug, out var ar)) continue;
            if (SetAr(n.Title, ar, out var t)) { n.Title = t; changed = true; }
        }

        // Offers: Title, keyed by slug
        var offerAr = new Dictionary<string, string>
        {
            ["current-offers"] = "العروض الحالية",
        };
        foreach (var o in await db.Offers.ToListAsync())
        {
            if (!offerAr.TryGetValue(o.Slug, out var ar)) continue;
            if (SetAr(o.Title, ar, out var t)) { o.Title = t; changed = true; }
        }

        if (changed) await db.SaveChangesAsync();
    }
```

> **Note on `Tagline`:** confirm the `Vehicle` entity has a `LocalizedText Tagline` property (it is referenced in `Views/Vehicles/Index.cshtml`). If the property name differs, adjust the two `v.Tagline` lines to match. If `Tagline` does not exist as a property, drop the Tagline lines (Name-only) and note it in the handoff — do NOT add a new column in this phase (that is a migration).

- [ ] **Step 3: Build**

Run: `dotnet build Solution/GAC.sln -c Debug`
Expected: succeeds.

- [ ] **Step 4: Commit**

```bash
git add Solution/GAC.Infrastructure/Data/ContentSeeder.cs
git commit -m "feat(phase4): seed Arabic content via idempotent backfill"
```

---

## Task 4: RTL stylesheet + Cairo webfont

Fill `rtl.css` (loaded only when `isRtl`, already wired in `_Layout`) and load Cairo for Arabic. The strategy: `<html dir="rtl">` already mirrors normal block/inline flow; `rtl.css` overrides the **physical** declarations in `styles.css` (left↔right). For JS sliders, force the track to `direction: ltr` so `main.js`'s `translateX(-Npx)` math stays correct, and restore `rtl` on card text.

**Files:**
- Modify: `GAC.Web/Views/Shared/_Layout.cshtml`
- Modify: `GAC.Web/wwwroot/assets/css/rtl.css`

- [ ] **Step 1: Load Cairo (RTL only) in `_Layout`**

In `Views/Shared/_Layout.cshtml`, inside the existing `@if (isRtl) { ... }` block, ADD the Cairo font link BEFORE the `rtl.css` link:
```cshtml
@if (isRtl)
{
    <link href="https://fonts.googleapis.com/css2?family=Cairo:wght@@400;500;600;700;800&display=swap" rel="stylesheet" />
    <link rel="stylesheet" href="~/assets/css/rtl.css" asp-append-version="true" />
}
```
(Remember: `@@` for the literal `@` in the Google Fonts URL.)

- [ ] **Step 2: Write `rtl.css`**

Replace the entire contents of `GAC.Web/wwwroot/assets/css/rtl.css` with the overrides below. These are scoped under `[dir="rtl"]` so they only apply in Arabic. They cover the directional rules inventoried from `styles.css`. After pasting, the implementer MUST audit the remaining `.mp-*` (model-page) rules in `styles.css` (search for `left`, `right`, `margin-left`, `padding-left`, `border-left`, `float`, `text-align`, `translateX`) and add mirrored overrides following the same pattern; the Task 6 visual pass confirms completeness.

```css
/* ===========================================================================
   RTL overrides (Phase 4). Loaded only when <html dir="rtl">.
   Mirrors the physical-direction declarations in styles.css.
   Sliders keep direction:ltr so main.js translateX math is unchanged.
   =========================================================================== */

/* ---- Arabic typography ---- */
[dir="rtl"] body { font-family: "Cairo", "Inter", system-ui, -apple-system, "Segoe UI", Roboto, Arial, sans-serif; }
[dir="rtl"] h1, [dir="rtl"] h2, [dir="rtl"] h3, [dir="rtl"] h4,
[dir="rtl"] .hero__title, [dir="rtl"] .spec__value,
[dir="rtl"] .offer-card__title { font-family: "Cairo", "Montserrat", system-ui, sans-serif; letter-spacing: 0; }

/* ---- Global text direction ---- */
[dir="rtl"] { text-align: right; }
[dir="rtl"] .crumb-bar .crumb-trail,
[dir="rtl"] .bas-hero__title h1,
[dir="rtl"] .fin-intro h1,
[dir="rtl"] .fin-banner h2 { text-align: right; }

/* ---- Header: brandbar / lang / nav ---- */
[dir="rtl"] .brandbar__franchise { margin-left: 0; margin-right: 6px; }
[dir="rtl"] .lang-menu { right: auto; left: 0; }
[dir="rtl"] .menu > li > a::after { transform-origin: right; }
[dir="rtl"] .has-drop > .drop { left: auto; right: 0; }
[dir="rtl"] .megamenu { left: 0; right: 0; }
[dir="rtl"] .megamenu__item .is-new { margin-left: 0; margin-right: 5px; }
[dir="rtl"] .megamenu__tab.is-active::after { left: 0; right: 0; }

/* divider between nav groups (styles.css ~166-167) */
[dir="rtl"] .mainnav__inner .menu { padding-left: 0; }

/* ---- Mobile drawer: slides from the right in RTL ---- */
[dir="rtl"] .drawer__panel { right: auto; left: 0; }

/* ---- Action dock: dock to the left edge in RTL ---- */
[dir="rtl"] .action-dock { right: auto; left: 20px; }
@media (min-width: 1101px) { [dir="rtl"] .footer-socials { padding-right: 0; padding-left: 56px; } }

/* ---- Back-to-top button ---- */
[dir="rtl"] .back-top { right: auto; left: 20px; }

/* ---- Carousels / sliders: keep LTR track so main.js math holds ---- */
[dir="rtl"] .carousel__track,
[dir="rtl"] .newscar__track,
[dir="rtl"] .hero__track,
[dir="rtl"] [data-carousel] .carousel__track { direction: ltr; }
/* restore RTL for readable card text */
[dir="rtl"] .model-card,
[dir="rtl"] .listing-card__body,
[dir="rtl"] .hero__inner { direction: rtl; }
/* swap prev/next arrow positions */
[dir="rtl"] .carousel__btn--prev { left: auto; right: -22px; }
[dir="rtl"] .carousel__btn--next { right: auto; left: -22px; }
[dir="rtl"] .newscar__arrow--prev { left: auto; right: -22px; }
[dir="rtl"] .newscar__arrow--next { right: auto; left: -22px; }

/* ---- Cards / badges (top-left → top-right) ---- */
[dir="rtl"] .offer-card__badge,
[dir="rtl"] .lineup-card__badge,
[dir="rtl"] .listing-card__cat { left: auto; right: 12px; }
[dir="rtl"] .offer-card__badge { right: 16px; }

/* ---- Spec table borders ---- */
[dir="rtl"] .spec { border-right: 0; border-left: 1px solid var(--c-line); }
[dir="rtl"] .spec:last-child { border-left: 0; }
[dir="rtl"] .specs .spec:nth-child(2n) { border-left: 0; }
[dir="rtl"] .spec__unit { margin-left: 0; margin-right: 4px; }

/* ---- Misc directional accents ---- */
[dir="rtl"] .callout { border-left: 0; border-right: 3px solid var(--c-accent); }
[dir="rtl"] .crumb-bar .crumb-trail { border-left: 0; border-right: 1px solid rgba(255,255,255,.22); padding-left: 0; padding-right: 16px; }
[dir="rtl"] .warr-banner__label { left: auto; right: 24px; }
[dir="rtl"] .location h3 + *::after,
[dir="rtl"] .dir-actions a::after { margin-left: 0; margin-right: auto; }

/* ---- Model-page (mp-*) basics; AUDIT styles.css for the rest ---- */
[dir="rtl"] .mp-subnav__links { margin-left: 0; margin-right: auto; }
[dir="rtl"] .mp-feature__list { padding-left: 0; padding-right: 1.1em; }
[dir="rtl"] .mp-gallery { margin-left: 0; margin-right: calc(50% - 50vw); }
@media (min-width:1200px){ [dir="rtl"] .mp-head--left { padding-right: 0; padding-left: 14%; } }
```

- [ ] **Step 3: Build**

Run: `dotnet build Solution/GAC.sln -c Debug`
Expected: succeeds (CSS is static; build just packages it).

- [ ] **Step 4: Commit**

```bash
git add Solution/GAC.Web/Views/Shared/_Layout.cshtml Solution/GAC.Web/wwwroot/assets/css/rtl.css
git commit -m "feat(phase4): add RTL stylesheet and Cairo Arabic font"
```

---

## Task 5: Tests

Verify Arabic seeding and Arabic+RTL rendering. Reuse the existing `DevWebApplicationFactory` for integration tests (it loads the real connection string; the seeder runs at startup).

**Files:**
- Create: `GAC.Tests/ArabicSeedTests.cs`
- Create: `GAC.Tests/ArabicRenderTests.cs`

- [ ] **Step 1: Seeder Arabic test (in-memory, fresh DB)**

`GAC.Tests/ArabicSeedTests.cs`:
```csharp
using GAC.Core.Content;
using GAC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GAC.Tests;

public class ArabicSeedTests
{
    private static ServiceProvider BuildServices(string dbName)
    {
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase(dbName));
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Seeds_ArabicForVehiclesAndMenuAndPages()
    {
        var sp = BuildServices("seed-arabic");
        await ContentSeeder.SeedAsync(sp);
        var db = sp.GetRequiredService<ApplicationDbContext>();

        var gs8 = await db.Vehicles.SingleAsync(v => v.Slug == "gs8");
        Assert.False(string.IsNullOrWhiteSpace(gs8.Name.Ar));
        Assert.Equal("GS8", gs8.Name.En);

        var home = await db.MenuItems.SingleAsync(m => m.Label!.En == "Home");
        Assert.Equal("الرئيسية", home.Label!.Ar);

        var about = await db.ContentPages.SingleAsync(p => p.Slug == "about");
        Assert.False(string.IsNullOrWhiteSpace(about.Title.Ar));

        var news = await db.NewsArticles.FirstAsync();
        Assert.False(string.IsNullOrWhiteSpace(news.Title.Ar));
    }

    [Fact]
    public async Task Backfill_IsIdempotent_AndPreservesExistingArabic()
    {
        var sp = BuildServices("seed-arabic-idem");
        await ContentSeeder.SeedAsync(sp);
        await ContentSeeder.SeedAsync(sp); // run twice
        var db = sp.GetRequiredService<ApplicationDbContext>();

        Assert.Equal(11, await db.Vehicles.CountAsync());
        var gs8 = await db.Vehicles.SingleAsync(v => v.Slug == "gs8");
        Assert.Equal("GS8", gs8.Name.Ar); // unchanged on second pass
    }
}
```

- [ ] **Step 2: Run it**

Run: `dotnet test Solution/GAC.sln --filter FullyQualifiedName~ArabicSeedTests`
Expected: 2 passing.

- [ ] **Step 3: Arabic render integration test**

`GAC.Tests/ArabicRenderTests.cs` — drive the site with the `ar` culture cookie and assert RTL + Arabic. Match the cookie format the existing `CookieRequestCultureProvider` expects (`c=ar|uic=ar`). Mirror the setup style of the existing `HomePageSmokeTests`/`RoutingTests`.
```csharp
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Localization;
using Xunit;

namespace GAC.Tests;

public class ArabicRenderTests : IClassFixture<DevWebApplicationFactory>
{
    private readonly DevWebApplicationFactory _factory;
    public ArabicRenderTests(DevWebApplicationFactory factory) => _factory = factory;

    private HttpClient ArabicClient()
    {
        var client = _factory.CreateClient();
        var cookieValue = CookieRequestCultureProvider.MakeCookieValue(new RequestCulture("ar"));
        client.DefaultRequestHeaders.Add("Cookie", $"{CookieRequestCultureProvider.DefaultCookieName}={cookieValue}");
        return client;
    }

    [Fact]
    public async Task Home_InArabic_IsRtl_LoadsRtlCssAndCairo()
    {
        var html = await ArabicClient().GetStringAsync("/");
        Assert.Contains("dir=\"rtl\"", html);
        Assert.Contains("rtl.css", html);
        Assert.Contains("family=Cairo", html);
    }

    [Fact]
    public async Task Home_InArabic_RendersArabicChrome()
    {
        var html = await ArabicClient().GetStringAsync("/");
        Assert.Contains("الرئيسية", html);   // Home menu label (DB)
        Assert.Contains("الموديلات", html);  // Models menu label (DB)
    }

    [Fact]
    public async Task Home_InEnglish_IsLtr_NoRtlAssets()
    {
        var html = await _factory.CreateClient().GetStringAsync("/");
        Assert.Contains("dir=\"ltr\"", html);
        Assert.DoesNotContain("rtl.css", html);
    }
}
```

- [ ] **Step 4: Run the full suite**

Run: `dotnet test Solution/GAC.sln`
Expected: all green (60 prior + new). Integration tests need the DB reachable.

- [ ] **Step 5: Commit**

```bash
git add Solution/GAC.Tests/ArabicSeedTests.cs Solution/GAC.Tests/ArabicRenderTests.cs
git commit -m "test(phase4): cover Arabic seeding and RTL rendering"
```

---

## Task 6: Visual parity pass (EN + AR) + handoff update

Run the app and visually verify both languages on representative pages, fixing any RTL glitches the static overrides missed (especially `.mp-*` model pages). Then update the handoff + memory.

**Files:**
- Modify (as needed): `GAC.Web/wwwroot/assets/css/rtl.css`
- Modify: `Solution/docs/HANDOFF.md`

- [ ] **Step 1: Run the site**

Run: `dotnet run --project Solution/GAC.Web`
Open in a browser. Toggle language via the header switch (writes the cookie).

- [ ] **Step 2: Check these pages in BOTH EN and AR**

For each, confirm: chrome mirrors correctly, text is right-aligned in AR, Cairo renders, sliders/carousels scroll the right way, no horizontal overflow, badges/arrows on the correct side.
- `/` (home: hero slider, model carousels, news)
- `/models` (listing grid + filter chips)
- `/gs8` (a vehicle detail / `.mp-*` page — body prose stays English by design; layout must still be RTL)
- `/news`, `/about`, `/contact-us`, `/offers`

- [ ] **Step 3: Fix any RTL gaps in `rtl.css`**

For each glitch, find the offending physical rule in `styles.css` and add a mirrored `[dir="rtl"]` override. Re-check. Commit CSS fixes:
```bash
git add Solution/GAC.Web/wwwroot/assets/css/rtl.css
git commit -m "fix(phase4): RTL visual parity adjustments"
```

- [ ] **Step 4: Update HANDOFF.md**

In `Solution/docs/HANDOFF.md`: mark Phase 4 ✅ in §4; add a "Phase 4 — what was built" section (resx localizer, Arabic backfill, rtl.css + Cairo, the `direction:ltr` slider technique); update deferred-item #5 (rtl.css now filled, Arabic seeded) and note the OUT-of-scope body prose still pending Phase 6 admin; set "Next" to Phase 5 (Forms & leads).

```bash
git add Solution/docs/HANDOFF.md
git commit -m "docs(phase4): update handoff for Arabic/RTL completion"
```

---

## Self-Review

**Spec coverage** (against the locked scope):
- RTL layout site-wide → Task 4 (`rtl.css` + `dir` already wired). ✅
- Cairo font → Task 4 Step 1. ✅
- Arabic for all DB content → Task 3 (`EnsureArabicAsync`, all 8 entity types + Tagline). ✅
- Arabic for static chrome/view strings → Tasks 1–2 (resx + `@L[...]`). ✅
- EN+AR visual parity pass → Task 6. ✅
- OUT-of-scope body prose left English → respected (Tasks 2/3 touch only chrome + DB-bound titles; partials untouched). ✅
- Latin numerals retained → nothing changes numeral rendering. ✅
- Forms stay static, no admin → no form POST or admin work added. ✅

**Placeholder scan:** No "TBD"/"add appropriate". The one conditional is Task 3 Step 2's `Tagline` note — explicitly instructs verify-then-adjust/drop, with a concrete fallback, not a vague placeholder. The `rtl.css` `.mp-*` audit is a defined method (grep physical props → mirror) paired with the Task 6 visual gate, not an open-ended "style it."

**Type/name consistency:** `IHtmlLocalizer<SharedResource>` injected as `L` (Task 1 Step 3) and used as `@L[...]` (Task 2). resx keys in Task 1 Step 4 match every `@L["..."]` key referenced in Task 2 (WhatsApp, Call {0}, All/Sedan/SUV/EV, All Models, English, action-dock pairs, footer nav, Explore, Read More, News/Latest News, Find Your GAC, Search). `LocalizedText { En, Ar }` shape and `.Localize()` usage match Phase 2/3. `CookieRequestCultureProvider.MakeCookieValue` (Task 5) matches the provider configured in `Program.cs`. `DevWebApplicationFactory` reused as in existing tests.

**Risk note:** `Vehicle.Tagline` existence is the one unverified assumption (it IS referenced in `Views/Vehicles/Index.cshtml`, strongly implying it exists) — Task 3 Step 2 handles both outcomes.
