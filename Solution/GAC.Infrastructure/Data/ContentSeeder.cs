using GAC.Core.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GAC.Infrastructure.Data;

/// <summary>
/// Seeds EN-only content into the database. Idempotent: each section is guarded
/// by an existence check so re-running will not duplicate rows.
/// Arabic content is Phase 4.
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
        await SeedContentPagesAsync(db);
        await SeedFormPagesAsync(db);
        await SeedNewsArticlesAsync(db);
        await SeedOffersAsync(db);
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
    //  Vehicles (11) + Hero + Gallery thumbnail VehicleImage each
    // ──────────────────────────────────────────────
    private static async Task SeedVehiclesAsync(ApplicationDbContext db)
    {
        if (await db.Vehicles.AnyAsync()) return;

        var vehicles = new[]
        {
            MakeVehicle(1, "gs8traveller",    "GS8 Traveller", VehicleCategory.Suv,                     true,  "/assets/img/hero-gs8-traveller.png", "/assets/img/m-gs8-traveller.png"),
            MakeVehicle(2, "gs8",             "GS8",           VehicleCategory.Suv,                     true,  "/assets/img/m-gs8.jpg",            "/assets/img/m-gs8.jpg"),
            MakeVehicle(3, "gs3emzoom",       "EMZOOM",        VehicleCategory.Suv,                     true,  "/assets/img/hero-gs3-emzoom.jpg",  "/assets/img/m-gs3-emzoom.png"),
            MakeVehicle(4, "emkoo",           "EMKOO",         VehicleCategory.Suv,                     true,  "/assets/img/m-emkoo.png",          "/assets/img/m-emkoo.png"),
            MakeVehicle(5, "empow",           "EMPOW",         VehicleCategory.Sedan,                   true,  "/assets/img/m-empow.png",          "/assets/img/m-empow.png"),
            MakeVehicle(6, "m8",              "M8",            VehicleCategory.Suv,                     true,  "/assets/img/hero-m8.png",          "/assets/img/m-m8.png"),
            MakeVehicle(7, "empow-sport",     "EMPOW R",       VehicleCategory.Sedan,                   true,  "/assets/img/hero-empow-sport.jpg", "/assets/img/m-empow-sport.png"),
            MakeVehicle(8, "aion-v",          "AION V",        VehicleCategory.Suv | VehicleCategory.Ev, false, "/assets/img/hero-aion-v.jpg",      "/assets/img/m-aion-v.png"),
            MakeVehicle(9, "aion-es",         "AION ES",       VehicleCategory.Sedan | VehicleCategory.Ev, false, "/assets/img/hero-aion-es.jpg",    "/assets/img/m-aion-es.png"),
            MakeVehicle(10,"hyptec-ht",       "HYPTEC HT",     VehicleCategory.Suv | VehicleCategory.Ev, true,  "/assets/img/m-hyptec-ht.png",     "/assets/img/m-hyptec-ht.png"),
            MakeVehicle(11,"gs4",             "GS4 MAX",       VehicleCategory.Suv,                     true,  "/assets/img/hero-gs4.jpg",         "/assets/img/m-gs4.png"),
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

            // 6. More (group)
            new MenuItem
            {
                SortOrder = 6, Label = "More", Url = null,
                Children = new List<MenuItem>
                {
                    new MenuItem { SortOrder = 1, Label = "Fleet Sales", Url = "/fleet" },
                    new MenuItem { SortOrder = 2, Label = "Finance",     Url = "/finance" },
                }
            },
        };

        db.MenuItems.AddRange(items);
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
