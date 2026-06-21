using GAC.Core.Content;
using GAC.Core.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace GAC.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Trim> Trims => Set<Trim>();
    public DbSet<SpecGroup> SpecGroups => Set<SpecGroup>();
    public DbSet<SpecRow> SpecRows => Set<SpecRow>();
    public DbSet<ColorOption> ColorOptions => Set<ColorOption>();
    public DbSet<FeatureSection> FeatureSections => Set<FeatureSection>();
    public DbSet<VehicleImage> VehicleImages => Set<VehicleImage>();
    public DbSet<ContentPage> ContentPages => Set<ContentPage>();
    public DbSet<ContentSection> ContentSections => Set<ContentSection>();
    public DbSet<FormPage> FormPages => Set<FormPage>();
    public DbSet<NewsArticle> NewsArticles => Set<NewsArticle>();
    public DbSet<Offer> Offers => Set<Offer>();
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<HomePage> HomePages => Set<HomePage>();
    public DbSet<HeroSlide> HeroSlides => Set<HeroSlide>();
    public DbSet<SiteSettings> SiteSettings => Set<SiteSettings>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
    public DbSet<DockItem> DockItems => Set<DockItem>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);   // keep — configures Identity
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
