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
        await SeedPromoAsync(db);
        await SeedDualCardsAsync(db);
        await SeedMenuAsync(db);
        await SeedDockItemsAsync(db);
        await SeedContentPagesAsync(db);
        await SeedWarrantyAsync(db);
        await SeedRoadAssistanceAsync(db);
        await SeedFormPagesAsync(db);
        await EnsureFormBannersAsync(db);
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
            ["gn6"]          = ("GN6", "مساحة فاخرة"),
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
            // Backfill English and Arabic independently: each is filled only when
            // currently blank and a matching seed file exists. Most vehicles ship
            // English-only (Arabic left null → English fallback at render); a
            // vehicle with a SeedContent/vehicles/ar/<slug>.html file also gets Arabic.
            var newEn = string.IsNullOrWhiteSpace(v.BodyHtml?.En) ? ReadSeedBody("vehicles", v.Slug) : null;
            var newAr = string.IsNullOrWhiteSpace(v.BodyHtml?.Ar) ? ReadSeedBody("vehicles", v.Slug, "ar") : null;
            if (newEn is null && newAr is null) continue;
            v.BodyHtml = new LocalizedText
            {
                En = newEn ?? v.BodyHtml?.En ?? "",
                Ar = newAr ?? v.BodyHtml?.Ar
            };
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
            MakeVehicle(5, "gn6",             "GN6",           VehicleCategory.Suv,                     true,  "/assets/img/hero-gn6.jpg",         "/assets/img/m-gn6.png"),
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
    //  Home "Latest Offers" promo block (1:1 on the singleton HomePage).
    //  Write-only-when-empty: seeds only if no PromoSection exists.
    // ──────────────────────────────────────────────
    public static async Task SeedPromoAsync(ApplicationDbContext db)
    {
        if (await db.PromoSections.AnyAsync()) return;
        var home = await db.HomePages.FirstOrDefaultAsync();
        if (home is null) return;

        db.PromoSections.Add(new PromoSection
        {
            HomePageId  = home.Id,
            ImagePath   = "/assets/img/feature-gs8-traveller.jpg",
            Eyebrow     = new LocalizedText { En = "Promotions", Ar = "العروض الترويجية" },
            Heading     = new LocalizedText { En = "Latest Offers", Ar = "أحدث العروض" },
            Description  = new LocalizedText { En = "Discover our extensive selection of GAC offers and promotions from Mutawa Alkadi Automotive.", Ar = "اكتشف تشكيلتنا الواسعة من عروض وتخفيضات جي إيه سي من مطوع القاضي للسيارات." },
            CtaText     = new LocalizedText { En = "View Offers", Ar = "اطّلع على العروض" },
            CtaLink     = "/offers",
            Campaigns =
            {
                new PromoCampaign { SortOrder = 0, Text = new LocalizedText { En = "Buy now, pay after 2 years", Ar = "اشترِ الآن وادفع بعد سنتين" } },
                new PromoCampaign { SortOrder = 1, Text = new LocalizedText { En = "0% interest", Ar = "بدون فوائد" } },
            }
        });
        await db.SaveChangesAsync();
    }

    // ──────────────────────────────────────────────
    //  Home "dual" cards (3 fixed tiles on the singleton HomePage).
    //  Write-only-when-empty: seeds only if no DualCard exists.
    // ──────────────────────────────────────────────
    public static async Task SeedDualCardsAsync(ApplicationDbContext db)
    {
        if (await db.DualCards.AnyAsync()) return;
        var home = await db.HomePages.FirstOrDefaultAsync();
        if (home is null) return;

        db.DualCards.AddRange(
            new DualCard
            {
                HomePageId = home.Id, SortOrder = 0,
                ImagePath = "/assets/img/tile-locations.jpg", Link = "/contact-us",
                Eyebrow = new LocalizedText { En = "Our showrooms", Ar = "معارضنا" },
                Title = new LocalizedText { En = "Locations", Ar = "المواقع" },
                Description = new LocalizedText { En = "Mutawa Alkadi Automotive has a strong presence across Kuwait with a number of showrooms that stretch across the country.", Ar = "تتمتع مطوع القاضي للسيارات بحضور قوي في جميع أنحاء الكويت من خلال عدد من المعارض المنتشرة في كل أرجاء البلاد." },
                ButtonText = new LocalizedText { En = "Find Us", Ar = "أوجدنا" },
            },
            new DualCard
            {
                HomePageId = home.Id, SortOrder = 1,
                ImagePath = "/assets/img/tile-service.jpg", Link = "/book-a-service",
                Eyebrow = new LocalizedText { En = "Aftersales", Ar = "خدمة ما بعد البيع" },
                Title = new LocalizedText { En = "Book a service", Ar = "احجز صيانة" },
                Description = new LocalizedText { En = "We are here to help make sure your GAC is running smoothly so you can continue to drive worry-free.", Ar = "نحن هنا لمساعدتك في الحفاظ على سيارتك جي إيه سي بأفضل حال لتواصل القيادة دون قلق." },
                ButtonText = new LocalizedText { En = "Book Now", Ar = "احجز الآن" },
            },
            new DualCard
            {
                HomePageId = home.Id, SortOrder = 2,
                ImagePath = "/assets/img/tile-parts.jpg", Link = "#",
                Eyebrow = new LocalizedText { En = "GAC", Ar = "جي إيه سي" },
                Title = new LocalizedText { En = "Parts & Accessories", Ar = "قطع الغيار والإكسسوارات" },
                Description = new LocalizedText { En = "Browse our range of GAC accessories which are most suited to your needs, including functional and practical accessories.", Ar = "تصفّح مجموعتنا من إكسسوارات جي إيه سي الأنسب لاحتياجاتك، بما في ذلك الإكسسوارات العملية والوظيفية." },
                ButtonText = new LocalizedText { En = "Discover More", Ar = "اكتشف المزيد" },
            });
        await db.SaveChangesAsync();
    }

    // ──────────────────────────────────────────────
    //  Form-page banners. Write-only-when-empty: sets BannerImagePath to its
    //  current default per slug ONLY where the column is null/empty — never
    //  overwrites a value an editor already set.
    // ──────────────────────────────────────────────
    public static async Task EnsureFormBannersAsync(ApplicationDbContext db)
    {
        var defaults = new Dictionary<string, string>
        {
            ["book-a-service"]    = "/assets/img/book-a-service/hero.jpg",
            ["book-a-test-drive"] = "/assets/img/book-a-test-drive/hero.jpg",
            ["request-a-quote"]   = "/assets/img/book-a-test-drive/hero.jpg",
            ["fleet"]             = "/assets/img/fleet/vehicles.jpg",
            ["recall-enquiry"]    = "/assets/img/book-a-service/hero.jpg",
        };
        var changed = false;
        foreach (var (slug, img) in defaults)
        {
            var page = await db.FormPages.FirstOrDefaultAsync(f => f.Slug == slug);
            if (page is null) continue;
            if (string.IsNullOrEmpty(page.BannerImagePath)) { page.BannerImagePath = img; changed = true; }
        }
        if (changed) await db.SaveChangesAsync();
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
    //  Warranty page (singleton). Write-only-when-empty: seeds the structured
    //  content from the current /warranty markup so the live page is identical
    //  the moment the migration is applied, then becomes editable. The cars grid
    //  is rendered dynamically from the visible Vehicles (not seeded here).
    // ──────────────────────────────────────────────
    public static async Task SeedWarrantyAsync(ApplicationDbContext db)
    {
        if (await db.WarrantyPages.AnyAsync()) return;

        db.WarrantyPages.Add(new WarrantyPage
        {
            BannerImagePath = "/assets/img/warranty/banner.jpg",
            BannerLabel = new LocalizedText { En = "GAC Mutawa Alkadi Automotive Warranty", Ar = "ضمان جي إيه سي مطوع القاضي للسيارات" },
            Heading = new LocalizedText { En = "Warranty", Ar = "الضمان" },
            Intro = new LocalizedText
            {
                En = "We, at GAC Mutawa Alkadi Automotive, believe that a premium and reliable vehicle should also be backed up by reliable warranty options.\nWe provide professional services delivered by professional experts by offering GAC Motor genuine spare parts and accessories at our professional service facility.",
                Ar = "نحن في جي إيه سي مطوع القاضي للسيارات نؤمن بأن السيارة الفاخرة والموثوقة يجب أن تكون مدعومة أيضاً بخيارات ضمان موثوقة.\nنقدّم خدمات احترافية يؤديها خبراء محترفون من خلال توفير قطع غيار وإكسسوارات جي إيه سي موتور الأصلية في منشأة الخدمة الاحترافية لدينا."
            },
            TermsImagePath = "/assets/img/warranty/callout.jpg",
            TermsNote = new LocalizedText { En = "*terms and conditions apply", Ar = "*تطبق الشروط والأحكام" },
            ExtendedHeading = new LocalizedText { En = "Extended Warranty Program", Ar = "برنامج الضمان الممتد" },
            ExtendedIntro = new LocalizedText
            {
                En = "Buy your new AAC vehicle today with complete peace of mind and stay protected longer with Extended Warranty and Roadside Assistance Programs offered through our licensed partners!\nThe program is offered as a 1-Year or 2-Year Extended Warranty and/or Roadside Assistance Program offering AAC Customers peace of mind through extended time and mileage depending on brand and model purchased. Both, the Extended Warranty and Roadside Assistance Programs offered AAC are done so through credible 3rd party companies to match and mirror the terms and conditions offered by the manufacturing brands.\nFor as long as you are driving an AAC vehicle, know that we have your best interest and care in mind.",
                Ar = ""
            },
            ExtendedTableHtml = new LocalizedText
            {
                En = "<table class=\"datatable datatable--matrix\">\n  <thead>\n    <tr><th>Brand</th><th>Manufacturer Warranty</th><th>Manufacturer Roadside Assistance</th><th>Extended Warranty</th><th>Extended Roadside Assistance</th><th>View Extended Warranty Policy</th></tr>\n  </thead>\n  <tbody>\n    <tr><td>GAC</td><td>5 Years and/or 150,000 KM</td><td>—</td><td>+2 Years<br>+Unlimited Mileage</td><td>+2 Years<br>+Unlimited Mileage</td><td><a href=\"#\">Click Here</a></td></tr>\n    <tr><td>Chevrolet</td><td>3 Years and/or 100,000 KM</td><td>3 Years and/or 100,000 KM</td><td>+2 Years<br>+50,000 KM</td><td>+2 Years<br>+50,000 KM</td><td><a href=\"#\">Click Here</a></td></tr>\n    <tr><td>GMC</td><td>3 Years and/or 100,000 KM</td><td>3 Years and/or 100,000 KM</td><td>+2 Years<br>+50,000 KM</td><td>+2 Years<br>+50,000 KM</td><td><a href=\"#\">Click Here</a></td></tr>\n    <tr><td>Cadillac</td><td>4 Years and/or 100,000 KM</td><td>4 Years and/or 100,000 KM</td><td>+1 Year<br>+50,000 KM</td><td>+1 Year<br>+50,000 KM</td><td><a href=\"#\">Click Here</a></td></tr>\n  </tbody>\n</table>",
                Ar = ""
            },
            Callouts =
            {
                new WarrantyCallout { SortOrder = 0,
                    Lead = new LocalizedText { En = "5 years extended warranty, unlimited mileage", Ar = "ضمان ممتد 5 سنوات، كيلومترات غير محدودة" },
                    Text = new LocalizedText { En = "for Mutawa Alkadi showroom customers", Ar = "لعملاء معرض مطوع القاضي" } },
                new WarrantyCallout { SortOrder = 1,
                    Lead = new LocalizedText { En = "5 years or 150,000 Km", Ar = "5 سنوات أو 150,000 كم" },
                    Text = new LocalizedText { En = "whichever comes first for all other sale channels", Ar = "أيهما أقرب لجميع قنوات البيع الأخرى" } },
            }
        });
        await db.SaveChangesAsync();
    }

    // ──────────────────────────────────────────────
    //  Road-assistance page (singleton, structured). Write-only-when-empty.
    // ──────────────────────────────────────────────
    public static async Task SeedRoadAssistanceAsync(ApplicationDbContext db)
    {
        if (await db.RoadAssistancePages.AnyAsync()) return;

        db.RoadAssistancePages.Add(new RoadAssistancePage
        {
            Heading = new LocalizedText { En = "Roadside Assistance", Ar = "المساعدة على الطريق" },
            Intro = new LocalizedText
            {
                En = "You can rely on our roadside assistance to assist in a motoring emergency and get you to the nearest GAC Mutawa Alkadi Automotive centers.\nAll we ask is that your car is under warranty.",
                Ar = "يمكنك الاعتماد على خدمة المساعدة على الطريق لدينا لمساعدتك في حالات الطوارئ المرورية وإيصالك إلى أقرب مراكز جي إيه سي مطوع القاضي للسيارات.\nكل ما نطلبه هو أن تكون سيارتك تحت الضمان."
            },
            ContactLead = new LocalizedText { En = "Getting In Touch", Ar = "للتواصل معنا" },
            ContactText = new LocalizedText
            {
                En = "All you have to do is contact us on the phone numbers below and our team will be at your service:",
                Ar = "كل ما عليك فعله هو التواصل معنا على أرقام الهاتف أدناه وسيكون فريقنا في خدمتك:"
            },
            PhoneNumber = "1833334",
            CallButtonLabel = new LocalizedText { En = "Call 1833334", Ar = "اتصل 1833334" }
        });
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
    //  Offers (6 cards, bilingual). Write-only-when-empty.
    //  Public /offers renders these live; admin manages them.
    // ──────────────────────────────────────────────
    public static async Task SeedOffersAsync(ApplicationDbContext db)
    {
        // Retire the pre-wiring single placeholder (empty body, sole row) so it does
        // not render as a lone empty card. Real admin-entered offers are never touched.
        var existing = await db.Offers.ToListAsync();
        if (existing.Count == 1 && existing[0].Slug == "current-offers"
            && string.IsNullOrWhiteSpace(existing[0].Body?.En))
        {
            db.Offers.Remove(existing[0]);
            await db.SaveChangesAsync();
        }

        if (await db.Offers.AnyAsync()) return;

        // Each owned LocalizedText must be its OWN instance — EF owned entities cannot
        // be shared across owners (a shared instance reloads as null on all but one).
        static LocalizedText Enquire() => new() { En = "Enquire Now", Ar = "استفسر الآن" };
        db.Offers.AddRange(
            new Offer
            {
                Slug = "finance-gs8", SortOrder = 1, IsActive = true, ButtonLabel = Enquire(),
                Title = new LocalizedText { En = "0% APR on GS8", Ar = "تمويل 0% على GS8" },
                Body  = new LocalizedText { En = "Drive home the flagship 7-seat SUV with zero-interest finance over selected terms.", Ar = "اقتنِ سيارة الدفع الرباعي الرائدة بسبعة مقاعد بتمويل بدون فوائد على فترات محددة." }
            },
            new Offer
            {
                Slug = "cashback-empow-r", SortOrder = 2, IsActive = true, ButtonLabel = Enquire(),
                Title = new LocalizedText { En = "EMPOW R", Ar = "EMPOW R" },
                Body  = new LocalizedText { En = "Special cashback on the high-performance sports sedan, this month only.", Ar = "استرداد نقدي خاص على السيدان الرياضية عالية الأداء، هذا الشهر فقط." }
            },
            new Offer
            {
                Slug = "aion-es-launch", SortOrder = 3, IsActive = true, ButtonLabel = Enquire(),
                Title = new LocalizedText { En = "AION ES Launch", Ar = "إطلاق AION ES" },
                Body  = new LocalizedText { En = "Introductory pricing plus a complimentary home-charging package on the all-electric ES.", Ar = "أسعار تمهيدية مع باقة شحن منزلي مجانية على ES الكهربائية بالكامل." }
            },
            new Offer
            {
                Slug = "trade-in-emzoom", SortOrder = 4, IsActive = true, ButtonLabel = Enquire(),
                Title = new LocalizedText { En = "Upgrade to EMZOOM", Ar = "الترقية إلى EMZOOM" },
                Body  = new LocalizedText { En = "Boosted trade-in value when you move up to the 2026 EMZOOM.", Ar = "قيمة استبدال أعلى عند الترقية إلى EMZOOM 2026." }
            },
            new Offer
            {
                Slug = "service-package", SortOrder = 5, IsActive = true, ButtonLabel = Enquire(),
                Title = new LocalizedText { En = "Service Package", Ar = "باقة الصيانة" },
                Body  = new LocalizedText { En = "Prepaid service plans with genuine parts and certified GAC technicians.", Ar = "خطط صيانة مدفوعة مسبقاً بقطع غيار أصلية وفنيين معتمدين من جي إيه سي." }
            },
            new Offer
            {
                Slug = "business-fleet", SortOrder = 6, IsActive = true, ButtonLabel = Enquire(),
                Title = new LocalizedText { En = "Business & Fleet", Ar = "الأعمال والأساطيل" },
                Body  = new LocalizedText { En = "Tailored pricing and dedicated support for corporate and fleet customers.", Ar = "أسعار مخصصة ودعم متخصص لعملاء الشركات والأساطيل." }
            });

        await db.SaveChangesAsync();
    }
}
