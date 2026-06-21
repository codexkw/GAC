using System.Reflection;
using GAC.Core.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GAC.Infrastructure.Data;

/// <summary>
/// Seeds bilingual (EN + AR) content into the database. Idempotent: English inserts are
/// guarded by an existence check so re-running will not duplicate rows, and the Arabic
/// backfill (<see cref="EnsureArabicAsync"/>) only writes a field's Arabic when it is blank.
/// </summary>
public static class ContentSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();

        await SeedSiteSettingsAsync(db);
        await SeedVehiclesAsync(db);
        await SeedHomePageAsync(db);
        await SeedMenuAsync(db);
        await SeedDockItemsAsync(db);
        await SeedContentPagesAsync(db);
        await SeedFormPagesAsync(db);
        await SeedNewsArticlesAsync(db);
        await SeedOffersAsync(db);
        await EnsureArabicAsync(db);
        await EnsureBodiesAsync(db);
    }

    // ──────────────────────────────────────────────
    //  Arabic backfill (Phase 4). Idempotent: only sets a field's Arabic
    //  when it is currently null/empty. Matches rows by natural key so it
    //  works on both fresh and previously-seeded (EN-only) databases.
    // ──────────────────────────────────────────────
    private static async Task EnsureArabicAsync(ApplicationDbContext db)
    {
        // Returns true (and sets `result`) only when the field's Arabic is currently blank.
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
            ["gn6"]          = ("GN6", "دفع رباعي مدمج عملي"),
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

        // Menu items: Label, keyed by the English label
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
            if (m.Label is null || !menuAr.TryGetValue(m.Label.En ?? "", out var ar)) continue;
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

    // ──────────────────────────────────────────────
    //  HTML body backfill (Phase 6b). Idempotent: only sets a row's English
    //  BodyHtml when it is currently blank. Arabic left null → English fallback
    //  at render time. Source markup is embedded under SeedContent/<area>/<slug>.html.
    // ──────────────────────────────────────────────
    private static async Task EnsureBodiesAsync(ApplicationDbContext db)
    {
        var changed = false;

        foreach (var v in await db.Vehicles.ToListAsync())
        {
            if (!string.IsNullOrWhiteSpace(v.BodyHtml?.En)) continue;
            var html = ReadSeedBody("vehicles", v.Slug);
            if (html is null) continue; // hidden vehicles have no seed body
            v.BodyHtml = new LocalizedText { En = html };
            changed = true;
        }

        foreach (var p in await db.ContentPages.ToListAsync())
        {
            // Backfill English and Arabic independently: each is filled only when
            // currently blank and a matching seed file exists, so admin edits in
            // either language are preserved.
            var newEn = string.IsNullOrWhiteSpace(p.BodyHtml?.En) ? ReadSeedBody("content", p.Slug) : null;
            var newAr = string.IsNullOrWhiteSpace(p.BodyHtml?.Ar) ? ReadSeedBody("content", p.Slug, "ar") : null;
            if (newEn is null && newAr is null) continue;
            p.BodyHtml = new LocalizedText
            {
                En = newEn ?? p.BodyHtml?.En ?? "",
                Ar = newAr ?? p.BodyHtml?.Ar
            };
            changed = true;
        }

        // Only the contact-us "Locate Us" directory has a body; the 5 functional
        // form pages keep their server-rendered partials. English and Arabic are
        // backfilled independently (only when blank), preserving admin edits.
        foreach (var f in await db.FormPages.ToListAsync())
        {
            var newEn = string.IsNullOrWhiteSpace(f.BodyHtml?.En) ? ReadSeedBody("forms", f.Slug) : null;
            var newAr = string.IsNullOrWhiteSpace(f.BodyHtml?.Ar) ? ReadSeedBody("forms", f.Slug, "ar") : null;
            if (newEn is null && newAr is null) continue;
            f.BodyHtml = new LocalizedText
            {
                En = newEn ?? f.BodyHtml?.En ?? "",
                Ar = newAr ?? f.BodyHtml?.Ar
            };
            changed = true;
        }

        if (changed) await db.SaveChangesAsync();
    }

    private static readonly Assembly SeedAssembly = typeof(ContentSeeder).Assembly;

    /// <summary>Reads SeedContent/&lt;area&gt;/[&lt;culture&gt;/]&lt;slug&gt;.html as an embedded resource, or null if absent.
    /// Matches case-insensitively and treats '-'/'_' as equivalent to tolerate manifest-name mangling.
    /// Pass <paramref name="culture"/> (e.g. "ar") to read a language-specific body from the culture subfolder;
    /// omit for the default (English). The culture lives in a subfolder, not the filename, so MSBuild does not
    /// mistake it for a satellite-assembly culture token (which would hide it from the main manifest).</summary>
    private static string? ReadSeedBody(string area, string slug, string? culture = null)
    {
        static string Norm(string s) => s.Replace('-', '_').ToLowerInvariant();
        var path = string.IsNullOrEmpty(culture) ? $"{area}.{slug}" : $"{area}.{culture}.{slug}";
        var wanted = Norm($".SeedContent.{path}.html");
        var name = SeedAssembly.GetManifestResourceNames()
            .FirstOrDefault(n => Norm(n).EndsWith(wanted, StringComparison.Ordinal));
        if (name is null) return null;
        using var stream = SeedAssembly.GetManifestResourceStream(name)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    // ──────────────────────────────────────────────
    //  Site Settings
    // ──────────────────────────────────────────────
    private static async Task SeedSiteSettingsAsync(ApplicationDbContext db)
    {
        if (await db.SiteSettings.AnyAsync()) return;

        // Social links in HTML/partials/footer.html all use href="#" (placeholder).
        // They are left null until real URLs are provided.
        db.SiteSettings.Add(new SiteSettings
        {
            Phone = "1833334",
            WhatsApp = "1833334",
            FooterTagline = "GAC Mutawa Alkadi Automotive",
            InstagramUrl = null,
            FacebookUrl = null,
            TiktokUrl = null,
            SnapchatUrl = null,
            XUrl = null
        });

        await db.SaveChangesAsync();
    }

    // ──────────────────────────────────────────────
    //  Vehicles (12) + Hero + Gallery thumbnail VehicleImage each
    // ──────────────────────────────────────────────
    private static async Task SeedVehiclesAsync(ApplicationDbContext db)
    {
        if (await db.Vehicles.AnyAsync()) return;

        // Display order (home dropdown, model strip, mega-menu): EMZOOM, EMKOO, GS4 MAX,
        // HYPTEC HT, GN6, GS8, GS8 Traveller, M8, EMPOW, EMPOW R. Hidden EV concepts last.
        var vehicles = new[]
        {
            MakeVehicle(1, "gs3emzoom",       "EMZOOM",        VehicleCategory.Suv,                     true,  "/assets/img/hero-gs3-emzoom.jpg",  "/assets/img/m-gs3-emzoom.png"),
            MakeVehicle(2, "emkoo",           "EMKOO",         VehicleCategory.Suv,                     true,  "/assets/img/m-emkoo.png",          "/assets/img/m-emkoo.png"),
            MakeVehicle(3, "gs4",             "GS4 MAX",       VehicleCategory.Suv,                     true,  "/assets/img/hero-gs4.jpg",         "/assets/img/m-gs4.png"),
            MakeVehicle(4, "hyptec-ht",       "HYPTEC HT",     VehicleCategory.Suv | VehicleCategory.Ev, true,  "/assets/img/m-hyptec-ht.png",     "/assets/img/m-hyptec-ht.png"),
            MakeVehicle(5, "gn6",             "GN6",           VehicleCategory.Suv,                     true,  "/assets/img/hero-gs4.jpg",         "/assets/img/m-gs4.png"),
            MakeVehicle(6, "gs8",             "GS8",           VehicleCategory.Suv,                     true,  "/assets/img/m-gs8.jpg",            "/assets/img/m-gs8.jpg"),
            MakeVehicle(7, "gs8traveller",    "GS8 Traveller", VehicleCategory.Suv,                     true,  "/assets/img/hero-gs8-traveller.png", "/assets/img/m-gs8-traveller.png"),
            MakeVehicle(8, "m8",              "M8",            VehicleCategory.Suv,                     true,  "/assets/img/hero-m8.png",          "/assets/img/m-m8.png"),
            MakeVehicle(9, "empow",           "EMPOW",         VehicleCategory.Sedan,                   true,  "/assets/img/m-empow.png",          "/assets/img/m-empow.png"),
            MakeVehicle(10,"empow-sport",     "EMPOW R",       VehicleCategory.Sedan,                   true,  "/assets/img/hero-empow-sport.jpg", "/assets/img/m-empow-sport.png"),
            MakeVehicle(11,"aion-v",          "AION V",        VehicleCategory.Suv | VehicleCategory.Ev, false, "/assets/img/hero-aion-v.jpg",      "/assets/img/m-aion-v.png"),
            MakeVehicle(12,"aion-es",         "AION ES",       VehicleCategory.Sedan | VehicleCategory.Ev, false, "/assets/img/hero-aion-es.jpg",    "/assets/img/m-aion-es.png"),
        };

        db.Vehicles.AddRange(vehicles);
        await db.SaveChangesAsync();
    }

    private static Vehicle MakeVehicle(int sortOrder, string slug, string nameEn,
        VehicleCategory category, bool isVisible, string heroPath, string thumbPath)
    {
        return new Vehicle
        {
            Slug = slug,
            Category = category,
            SortOrder = sortOrder,
            IsVisible = isVisible,
            Name = nameEn,
            Images = new List<VehicleImage>
            {
                new VehicleImage
                {
                    Kind = VehicleImageKind.Hero,
                    Path = heroPath,
                    SortOrder = 0
                },
                new VehicleImage
                {
                    Kind = VehicleImageKind.Gallery,
                    Path = thumbPath,
                    SortOrder = 0
                }
            }
        };
    }

    // ──────────────────────────────────────────────
    //  HomePage + 9 HeroSlides
    // ──────────────────────────────────────────────
    private static async Task SeedHomePageAsync(ApplicationDbContext db)
    {
        if (await db.HomePages.AnyAsync()) return;

        var home = new HomePage
        {
            Slides = new List<HeroSlide>
            {
                MakeSlide(1, "/assets/img/hero-s7.jpg",             "GAC Motor",    null),
                MakeSlide(2, "/assets/img/hero-gs4.jpg",            "GS4 MAX",      "/gs4"),
                MakeSlide(3, "/assets/img/hero-hyptec-ht.jpg",      "HYPTEC HT",    "/hyptec-ht"),
                MakeSlide(4, "/assets/img/hero-aion-v.jpg",         "AION V",       "/aion-v"),
                MakeSlide(5, "/assets/img/hero-aion-es.jpg",        "AION ES",      "/aion-es"),
                MakeSlide(6, "/assets/img/hero-empow-sport.jpg",    "EMPOW R",      "/empow-sport"),
                MakeSlide(7, "/assets/img/hero-gs8-traveller.png",  "GS8 Traveller","/gs8traveller"),
                MakeSlide(8, "/assets/img/hero-m8.png",             "M8",           "/m8"),
                MakeSlide(9, "/assets/img/hero-gs3-emzoom.jpg",     "EMZOOM",       "/gs3emzoom"),
            }
        };

        db.HomePages.Add(home);
        await db.SaveChangesAsync();
    }

    private static HeroSlide MakeSlide(int sortOrder, string imagePath, string headingEn, string? ctaLink)
    {
        return new HeroSlide
        {
            SortOrder = sortOrder,
            ImagePath = imagePath,
            Heading = headingEn,
            CtaLink = ctaLink
        };
    }

    // ──────────────────────────────────────────────
    //  Menu (6 top-level items, some with children)
    // ──────────────────────────────────────────────
    private static async Task SeedMenuAsync(ApplicationDbContext db)
    {
        if (await db.MenuItems.AnyAsync()) return;

        var items = new[]
        {
            // 1. Home
            new MenuItem { SortOrder = 1, Label = "Home",           Url = "/" },

            // 2. Models
            new MenuItem { SortOrder = 2, Label = "Models",         Url = "/models" },

            // 3. Owners (group)
            new MenuItem
            {
                SortOrder = 3, Label = "Owners", Url = null,
                Children = new List<MenuItem>
                {
                    new MenuItem { SortOrder = 1, Label = "Book a Service",       Url = "/book-a-service" },
                    new MenuItem { SortOrder = 2, Label = "Cost of Service",      Url = "/cost-of-service" },
                    new MenuItem { SortOrder = 3, Label = "Warranty",             Url = "/warranty" },
                    new MenuItem { SortOrder = 4, Label = "Recall",               Url = "/recall-enquiry" },
                    new MenuItem { SortOrder = 5, Label = "Road-Side Assistance", Url = "/road-assistance" },
                }
            },

            // 4. Shopping Tools (group)
            new MenuItem
            {
                SortOrder = 4, Label = "Shopping Tools", Url = null,
                Children = new List<MenuItem>
                {
                    new MenuItem { SortOrder = 1, Label = "Book a Test Drive", Url = "/book-a-test-drive" },
                    new MenuItem { SortOrder = 2, Label = "Request a Quote",   Url = "/request-a-quote" },
                }
            },

            // 5. Locations
            new MenuItem { SortOrder = 5, Label = "Locations", Url = "/contact-us" },

            // 6. Fleet Sales (single top-level link — replaced the old "More" dropdown)
            new MenuItem { SortOrder = 6, Label = "Fleet Sales", Url = "/fleet" },
        };

        db.MenuItems.AddRange(items);
        await db.SaveChangesAsync();
    }

    // ──────────────────────────────────────────────
    //  Action-dock (6 items — matches the previously-hardcoded footer dock)
    // ──────────────────────────────────────────────
    private static async Task SeedDockItemsAsync(ApplicationDbContext db)
    {
        if (await db.DockItems.AnyAsync()) return;

        db.DockItems.AddRange(
            new DockItem
            {
                SortOrder = 1, Icon = "whatsapp", LinkType = DockLinkType.WhatsApp,
                Label = new() { En = "Chat on WhatsApp", Ar = "تواصل عبر واتساب" },
                ShortLabel = new() { En = "WhatsApp", Ar = "واتساب" }
            },
            new DockItem
            {
                SortOrder = 2, Icon = "test-drive", LinkType = DockLinkType.Url, Url = "/book-a-test-drive",
                Label = new() { En = "Book a Test Drive", Ar = "احجز تجربة قيادة" },
                ShortLabel = new() { En = "Test Drive", Ar = "تجربة قيادة" }
            },
            new DockItem
            {
                SortOrder = 3, Icon = "quote", LinkType = DockLinkType.Url, Url = "/request-a-quote",
                Label = new() { En = "Get Online Quote", Ar = "اطلب عرض سعر" },
                ShortLabel = new() { En = "Quote", Ar = "عرض سعر" }
            },
            new DockItem
            {
                SortOrder = 4, Icon = "brochure", LinkType = DockLinkType.VehicleBrochure,
                Label = new() { En = "Download Brochure", Ar = "حمّل الكتيب" },
                ShortLabel = new() { En = "Brochure", Ar = "الكتيب" }
            },
            new DockItem
            {
                SortOrder = 5, Icon = "location", LinkType = DockLinkType.Url, Url = "/contact-us",
                Label = new() { En = "Find Showroom", Ar = "أوجد المعرض" },
                ShortLabel = new() { En = "Showroom", Ar = "المعرض" }
            },
            new DockItem
            {
                SortOrder = 6, Icon = "mail", LinkType = DockLinkType.Url, Url = "/contact-us",
                Label = new() { En = "Contact Us", Ar = "تواصل معنا" },
                ShortLabel = new() { En = "Contact", Ar = "تواصل" }
            }
        );
        await db.SaveChangesAsync();
    }

    // ──────────────────────────────────────────────
    //  Content Pages (6 — news/offers owned by dedicated controllers)
    // ──────────────────────────────────────────────
    private static async Task SeedContentPagesAsync(ApplicationDbContext db)
    {
        if (await db.ContentPages.AnyAsync()) return;

        var pages = new[]
        {
            new ContentPage { Slug = "about",             Title = "About Us" },
            new ContentPage { Slug = "warranty",          Title = "Warranty" },
            new ContentPage { Slug = "privacy-policy",    Title = "Privacy Policy" },
            new ContentPage { Slug = "finance",           Title = "Tayseer Finance" },
            new ContentPage { Slug = "cost-of-service",   Title = "Cost of Service" },
            new ContentPage { Slug = "road-assistance",   Title = "Roadside Assistance" },
        };

        db.ContentPages.AddRange(pages);
        await db.SaveChangesAsync();
    }

    // ──────────────────────────────────────────────
    //  Form Pages (6)
    // ──────────────────────────────────────────────
    private static async Task SeedFormPagesAsync(ApplicationDbContext db)
    {
        if (await db.FormPages.AnyAsync()) return;

        var pages = new[]
        {
            new FormPage { Slug = "book-a-service",    FormType = FormType.ServiceBooking, Title = "Book a Service" },
            new FormPage { Slug = "book-a-test-drive", FormType = FormType.TestDrive,      Title = "Book a Test Drive" },
            new FormPage { Slug = "request-a-quote",   FormType = FormType.Quote,          Title = "Request a Quote" },
            new FormPage { Slug = "contact-us",        FormType = FormType.Contact,        Title = "Locate Us" },
            new FormPage { Slug = "fleet",             FormType = FormType.Fleet,          Title = "Fleet" },
            new FormPage { Slug = "recall-enquiry",    FormType = FormType.RecallEnquiry,  Title = "Recall Enquiry" },
        };

        db.FormPages.AddRange(pages);
        await db.SaveChangesAsync();
    }

    // ──────────────────────────────────────────────
    //  News Articles (3 — titles from index.html)
    // ──────────────────────────────────────────────
    private static async Task SeedNewsArticlesAsync(ApplicationDbContext db)
    {
        if (await db.NewsArticles.AnyAsync()) return;

        var articles = new[]
        {
            new NewsArticle
            {
                SortOrder   = 1,
                Slug        = "gac-empow-2026-high-performance-sports-sedan",
                Title       = "GAC EMPOW 2026: The High-Performance Sports Sedan with a New Engine",
                ImagePath   = "/assets/img/news-empow.jpg",
                PublishedOn = new DateOnly(2026, 1, 1),
                IsPublished = true
            },
            new NewsArticle
            {
                SortOrder   = 2,
                Slug        = "mutawa-alkadi-intensive-training-technical-competition",
                Title       = "Mutawa Alkadi Automotive Company Conducts Intensive Training and Technical Competition for GAC Motor Technicians",
                ImagePath   = "/assets/img/news-training.jpg",
                PublishedOn = new DateOnly(2026, 1, 1),
                IsPublished = true
            },
            new NewsArticle
            {
                SortOrder   = 3,
                Slug        = "emzoom-first-chinese-car-quality-ranking-2024",
                Title       = "Mutawa Alkadi Automotive Company Announces EMZOOM the First in Chinese Car Quality Ranking for 2024's First Half",
                ImagePath   = "/assets/img/news-gs3-award.jpg",
                PublishedOn = new DateOnly(2026, 1, 1),
                IsPublished = true
            },
        };

        db.NewsArticles.AddRange(articles);
        await db.SaveChangesAsync();
    }

    // ──────────────────────────────────────────────
    //  Offers (1)
    // ──────────────────────────────────────────────
    private static async Task SeedOffersAsync(ApplicationDbContext db)
    {
        if (await db.Offers.AnyAsync()) return;

        db.Offers.Add(new Offer
        {
            Slug      = "current-offers",
            Title     = "Current Offers",
            IsActive  = true,
            SortOrder = 1
        });

        await db.SaveChangesAsync();
    }
}
