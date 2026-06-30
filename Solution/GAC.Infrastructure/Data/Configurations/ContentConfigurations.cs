using GAC.Core.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

// Task 9: group→child configs (SliderGroup/Slide, GalleryTab/Image, FeatureBullet, TrimPriceRow, QualityBlock)
namespace GAC.Infrastructure.Data.Configurations;

internal static class OwnedExtensions
{
    public static void OwnsLocalized<TEntity>(
        this EntityTypeBuilder<TEntity> b,
        System.Linq.Expressions.Expression<System.Func<TEntity, LocalizedText?>> nav)
        where TEntity : class
    {
        b.OwnsOne(nav, o =>
        {
            o.Property(p => p.En);
            o.Property(p => p.Ar);
        });
        b.Navigation(nav).IsRequired();
    }
}

public class VehicleConfig : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> b)
    {
        b.HasIndex(v => v.Slug).IsUnique();
        b.Property(v => v.Slug).HasMaxLength(100).IsRequired();
        b.Property(v => v.PriceFrom).HasColumnType("decimal(18,2)");
        b.OwnsLocalized(v => v.Name);
        b.OwnsLocalized(v => v.Tagline);
        b.OwnsLocalized(v => v.IntroText);
        b.OwnsLocalized(v => v.BodyHtml);
        b.OwnsLocalized(v => v.MetaTitle);
        b.OwnsLocalized(v => v.MetaDescription);
        b.HasMany(v => v.Images).WithOne().HasForeignKey(i => i.VehicleId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(v => v.Trims).WithOne().HasForeignKey(t => t.VehicleId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(v => v.SpecGroups).WithOne().HasForeignKey(s => s.VehicleId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(v => v.Colors).WithOne().HasForeignKey(c => c.VehicleId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(v => v.Features).WithOne().HasForeignKey(f => f.VehicleId).OnDelete(DeleteBehavior.Cascade);
        b.OwnsLocalized(v => v.StatsNote);
        b.OwnsLocalized(v => v.EnquiryTitle);
        b.OwnsLocalized(v => v.EnquirySub);
        b.OwnsLocalized(v => v.EnquiryLead);
        b.Property(v => v.TechBannerImage).HasMaxLength(300);
        b.Property(v => v.EnquiryBgImage).HasMaxLength(300);
        b.HasMany(v => v.Headings).WithOne().HasForeignKey(h => h.VehicleId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(v => v.Stats).WithOne().HasForeignKey(s => s.VehicleId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(v => v.Sliders).WithOne().HasForeignKey(s => s.VehicleId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(v => v.GalleryTabs).WithOne().HasForeignKey(g => g.VehicleId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(v => v.Cards).WithOne().HasForeignKey(c => c.VehicleId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(v => v.SafetyToggles).WithOne().HasForeignKey(s => s.VehicleId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(v => v.WarrantyLinks).WithOne().HasForeignKey(w => w.VehicleId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(v => v.Quality).WithOne().HasForeignKey<QualityBlock>(q => q.VehicleId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class VehicleImageConfig : IEntityTypeConfiguration<VehicleImage>
{
    public void Configure(EntityTypeBuilder<VehicleImage> b)
    {
        b.Property(i => i.Path).HasMaxLength(300).IsRequired();
        b.OwnsLocalized(i => i.Alt);
    }
}

public class TrimConfig : IEntityTypeConfiguration<Trim>
{
    public void Configure(EntityTypeBuilder<Trim> b)
    {
        b.Property(t => t.Price).HasColumnType("decimal(18,2)");
        b.Property(t => t.ImagePath).HasMaxLength(300);
        b.OwnsLocalized(t => t.Name);
        b.OwnsLocalized(t => t.Highlights);
        b.OwnsLocalized(t => t.ModelLabel);
        b.HasMany(t => t.PriceRows).WithOne().HasForeignKey(x => x.TrimId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class SpecGroupConfig : IEntityTypeConfiguration<SpecGroup>
{
    public void Configure(EntityTypeBuilder<SpecGroup> b)
    {
        b.OwnsLocalized(s => s.Title);
        b.HasMany(s => s.Rows).WithOne().HasForeignKey(r => r.SpecGroupId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class SpecRowConfig : IEntityTypeConfiguration<SpecRow>
{
    public void Configure(EntityTypeBuilder<SpecRow> b)
    {
        b.OwnsLocalized(r => r.Label);
        b.OwnsLocalized(r => r.Value);
    }
}

public class ColorOptionConfig : IEntityTypeConfiguration<ColorOption>
{
    public void Configure(EntityTypeBuilder<ColorOption> b)
    {
        b.Property(c => c.Hex).HasMaxLength(9);
        b.OwnsLocalized(c => c.Name);
    }
}

public class FeatureSectionConfig : IEntityTypeConfiguration<FeatureSection>
{
    public void Configure(EntityTypeBuilder<FeatureSection> b)
    {
        b.OwnsLocalized(f => f.Heading);
        b.OwnsLocalized(f => f.Body);
        b.OwnsLocalized(f => f.TabLabel);
        b.OwnsLocalized(f => f.Lead);
        b.HasMany(f => f.Bullets).WithOne().HasForeignKey(x => x.FeatureSectionId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ContentPageConfig : IEntityTypeConfiguration<ContentPage>
{
    public void Configure(EntityTypeBuilder<ContentPage> b)
    {
        b.HasIndex(p => p.Slug).IsUnique();
        b.Property(p => p.Slug).HasMaxLength(100).IsRequired();
        b.OwnsLocalized(p => p.Title);
        b.OwnsLocalized(p => p.BodyHtml);
        b.OwnsLocalized(p => p.MetaTitle);
        b.OwnsLocalized(p => p.MetaDescription);
        b.HasMany(p => p.Sections).WithOne().HasForeignKey(s => s.ContentPageId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ContentSectionConfig : IEntityTypeConfiguration<ContentSection>
{
    public void Configure(EntityTypeBuilder<ContentSection> b)
    {
        b.OwnsLocalized(s => s.Heading);
        b.OwnsLocalized(s => s.Body);
    }
}

public class FormPageConfig : IEntityTypeConfiguration<FormPage>
{
    public void Configure(EntityTypeBuilder<FormPage> b)
    {
        b.HasIndex(p => p.Slug).IsUnique();
        b.Property(p => p.Slug).HasMaxLength(100).IsRequired();
        b.OwnsLocalized(p => p.Title);
        b.OwnsLocalized(p => p.IntroText);
        b.OwnsLocalized(p => p.BodyHtml);
        b.OwnsLocalized(p => p.MetaTitle);
        b.OwnsLocalized(p => p.MetaDescription);
        b.Property(p => p.BannerImagePath).HasMaxLength(300);
    }
}

public class NewsArticleConfig : IEntityTypeConfiguration<NewsArticle>
{
    public void Configure(EntityTypeBuilder<NewsArticle> b)
    {
        b.HasIndex(n => n.Slug).IsUnique();
        b.Property(n => n.Slug).HasMaxLength(120).IsRequired();
        b.OwnsLocalized(n => n.Title);
        b.OwnsLocalized(n => n.Excerpt);
        b.OwnsLocalized(n => n.Body);
    }
}

public class OfferConfig : IEntityTypeConfiguration<Offer>
{
    public void Configure(EntityTypeBuilder<Offer> b)
    {
        b.HasIndex(o => o.Slug).IsUnique();
        b.Property(o => o.Slug).HasMaxLength(120).IsRequired();
        b.OwnsLocalized(o => o.Title);
        b.OwnsLocalized(o => o.Body);
        b.OwnsLocalized(o => o.ButtonLabel);
    }
}

public class LeadConfig : IEntityTypeConfiguration<Lead>
{
    public void Configure(EntityTypeBuilder<Lead> b)
    {
        b.Property(l => l.Name).HasMaxLength(200).IsRequired();
        b.HasOne(l => l.Vehicle).WithMany().HasForeignKey(l => l.VehicleId).OnDelete(DeleteBehavior.SetNull);
        b.HasIndex(l => l.Status);
        b.HasIndex(l => l.CreatedAt);
    }
}

public class HomePageConfig : IEntityTypeConfiguration<HomePage>
{
    public void Configure(EntityTypeBuilder<HomePage> b)
    {
        b.HasMany(h => h.Slides).WithOne().HasForeignKey(s => s.HomePageId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(h => h.Promo).WithOne().HasForeignKey<PromoSection>(p => p.HomePageId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(h => h.DualCards).WithOne().HasForeignKey(c => c.HomePageId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class PromoSectionConfig : IEntityTypeConfiguration<PromoSection>
{
    public void Configure(EntityTypeBuilder<PromoSection> b)
    {
        b.Property(p => p.ImagePath).HasMaxLength(300).IsRequired();
        b.Property(p => p.CtaLink).HasMaxLength(300);
        b.OwnsLocalized(p => p.Eyebrow);
        b.OwnsLocalized(p => p.Heading);
        b.OwnsLocalized(p => p.Description);
        b.OwnsLocalized(p => p.CtaText);
        b.HasMany(p => p.Campaigns).WithOne().HasForeignKey(c => c.PromoSectionId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class PromoCampaignConfig : IEntityTypeConfiguration<PromoCampaign>
{
    public void Configure(EntityTypeBuilder<PromoCampaign> b)
    {
        b.OwnsLocalized(c => c.Text);
    }
}

public class DualCardConfig : IEntityTypeConfiguration<DualCard>
{
    public void Configure(EntityTypeBuilder<DualCard> b)
    {
        b.Property(c => c.ImagePath).HasMaxLength(300).IsRequired();
        b.Property(c => c.Link).HasMaxLength(300);
        b.OwnsLocalized(c => c.Eyebrow);
        b.OwnsLocalized(c => c.Title);
        b.OwnsLocalized(c => c.Description);
        b.OwnsLocalized(c => c.ButtonText);
    }
}

public class HeroSlideConfig : IEntityTypeConfiguration<HeroSlide>
{
    public void Configure(EntityTypeBuilder<HeroSlide> b)
    {
        b.Property(s => s.ImagePath).HasMaxLength(300).IsRequired();
        b.OwnsLocalized(s => s.Heading);
        b.OwnsLocalized(s => s.Subheading);
        b.OwnsLocalized(s => s.CtaText);
    }
}

public class SiteSettingsConfig : IEntityTypeConfiguration<SiteSettings>
{
    public void Configure(EntityTypeBuilder<SiteSettings> b)
    {
        b.OwnsLocalized(s => s.FooterTagline);
    }
}

public class MenuItemConfig : IEntityTypeConfiguration<MenuItem>
{
    public void Configure(EntityTypeBuilder<MenuItem> b)
    {
        b.OwnsLocalized(m => m.Label);
        b.HasMany(m => m.Children).WithOne(m => m.Parent!).HasForeignKey(m => m.ParentId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class MediaAssetConfig : IEntityTypeConfiguration<MediaAsset>
{
    public void Configure(EntityTypeBuilder<MediaAsset> b)
    {
        b.Property(m => m.Path).HasMaxLength(300).IsRequired();
        b.OwnsLocalized(m => m.Alt);
    }
}

public class DockItemConfig : IEntityTypeConfiguration<DockItem>
{
    public void Configure(EntityTypeBuilder<DockItem> b)
    {
        b.Property(d => d.Icon).HasMaxLength(50);
        b.Property(d => d.Url).HasMaxLength(300);
        b.OwnsLocalized(d => d.Label);
        b.OwnsLocalized(d => d.ShortLabel);
    }
}

public class SectionHeadingConfig : IEntityTypeConfiguration<SectionHeading>
{
    public void Configure(EntityTypeBuilder<SectionHeading> b)
    {
        b.OwnsLocalized(s => s.Title);
        b.OwnsLocalized(s => s.Sub);
        b.OwnsLocalized(s => s.Body);
    }
}

public class StatItemConfig : IEntityTypeConfiguration<StatItem>
{
    public void Configure(EntityTypeBuilder<StatItem> b)
    {
        b.OwnsLocalized(s => s.Label);
        b.OwnsLocalized(s => s.Value);
    }
}

public class CardItemConfig : IEntityTypeConfiguration<CardItem>
{
    public void Configure(EntityTypeBuilder<CardItem> b)
    {
        b.Property(c => c.ImagePath).HasMaxLength(300);
        b.OwnsLocalized(c => c.Title);
        b.OwnsLocalized(c => c.Text);
    }
}

public class SafetyToggleConfig : IEntityTypeConfiguration<SafetyToggle>
{
    public void Configure(EntityTypeBuilder<SafetyToggle> b)
    {
        b.Property(s => s.ImagePath).HasMaxLength(300);
        b.OwnsLocalized(s => s.Title);
        b.OwnsLocalized(s => s.Strap);
        b.OwnsLocalized(s => s.Content);
    }
}

public class WarrantyLinkConfig : IEntityTypeConfiguration<WarrantyLink>
{
    public void Configure(EntityTypeBuilder<WarrantyLink> b)
    {
        b.Property(w => w.Url).HasMaxLength(500).IsRequired();
        b.OwnsLocalized(w => w.Label);
    }
}

public class SliderGroupConfig : IEntityTypeConfiguration<SliderGroup>
{
    public void Configure(EntityTypeBuilder<SliderGroup> b)
    {
        b.OwnsLocalized(s => s.Eyebrow);
        b.OwnsLocalized(s => s.Title);
        b.HasMany(s => s.Slides).WithOne().HasForeignKey(x => x.SliderGroupId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class SliderSlideConfig : IEntityTypeConfiguration<SliderSlide>
{
    public void Configure(EntityTypeBuilder<SliderSlide> b)
    {
        b.Property(s => s.ImagePath).HasMaxLength(300);
        b.OwnsLocalized(s => s.Alt);
    }
}

public class GalleryTabConfig : IEntityTypeConfiguration<GalleryTab>
{
    public void Configure(EntityTypeBuilder<GalleryTab> b)
    {
        b.OwnsLocalized(g => g.Label);
        b.HasMany(g => g.Images).WithOne().HasForeignKey(x => x.GalleryTabId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class GalleryImageConfig : IEntityTypeConfiguration<GalleryImage>
{
    public void Configure(EntityTypeBuilder<GalleryImage> b)
    {
        b.Property(g => g.ImagePath).HasMaxLength(300);
        b.OwnsLocalized(g => g.Alt);
    }
}

public class FeatureBulletConfig : IEntityTypeConfiguration<FeatureBullet>
{
    public void Configure(EntityTypeBuilder<FeatureBullet> b)
    {
        b.OwnsLocalized(x => x.Label);
        b.OwnsLocalized(x => x.Text);
    }
}

public class TrimPriceRowConfig : IEntityTypeConfiguration<TrimPriceRow>
{
    public void Configure(EntityTypeBuilder<TrimPriceRow> b)
    {
        b.OwnsLocalized(x => x.Text);
    }
}

public class QualityBlockConfig : IEntityTypeConfiguration<QualityBlock>
{
    public void Configure(EntityTypeBuilder<QualityBlock> b)
    {
        b.Property(q => q.MainImage).HasMaxLength(300);
        b.Property(q => q.ThumbImage).HasMaxLength(300);
        b.OwnsLocalized(q => q.Strapline);
        b.OwnsLocalized(q => q.Content);
    }
}

public class WarrantyPageConfig : IEntityTypeConfiguration<WarrantyPage>
{
    public void Configure(EntityTypeBuilder<WarrantyPage> b)
    {
        b.Property(w => w.BannerImagePath).HasMaxLength(300).IsRequired();
        b.Property(w => w.TermsImagePath).HasMaxLength(300).IsRequired();
        b.OwnsLocalized(w => w.BannerLabel);
        b.OwnsLocalized(w => w.Heading);
        b.OwnsLocalized(w => w.Intro);
        b.OwnsLocalized(w => w.TermsNote);
        b.OwnsLocalized(w => w.ExtendedHeading);
        b.OwnsLocalized(w => w.ExtendedIntro);
        b.OwnsLocalized(w => w.ExtendedTableHtml);
        b.HasMany(w => w.Callouts).WithOne().HasForeignKey(c => c.WarrantyPageId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class WarrantyCalloutConfig : IEntityTypeConfiguration<WarrantyCallout>
{
    public void Configure(EntityTypeBuilder<WarrantyCallout> b)
    {
        b.OwnsLocalized(c => c.Lead);
        b.OwnsLocalized(c => c.Text);
    }
}

public class RoadAssistancePageConfig : IEntityTypeConfiguration<RoadAssistancePage>
{
    public void Configure(EntityTypeBuilder<RoadAssistancePage> b)
    {
        b.Property(r => r.PhoneNumber).HasMaxLength(40);
        b.OwnsLocalized(r => r.Heading);
        b.OwnsLocalized(r => r.Intro);
        b.OwnsLocalized(r => r.ContactLead);
        b.OwnsLocalized(r => r.ContactText);
        b.OwnsLocalized(r => r.CallButtonLabel);
    }
}

public class CostOfServicePageConfig : IEntityTypeConfiguration<CostOfServicePage>
{
    public void Configure(EntityTypeBuilder<CostOfServicePage> b)
    {
        b.Property(p => p.ButtonUrl).HasMaxLength(500);
        b.OwnsLocalized(p => p.Title);
        b.OwnsLocalized(p => p.ButtonLabel);
        b.OwnsLocalized(p => p.TableHeadLabel);
        b.OwnsLocalized(p => p.FooterNote);
        b.HasMany(p => p.Rows).WithOne().HasForeignKey(r => r.CostOfServicePageId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(p => p.Models).WithOne().HasForeignKey(m => m.CostOfServicePageId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class CostServiceRowConfig : IEntityTypeConfiguration<CostServiceRow>
{
    public void Configure(EntityTypeBuilder<CostServiceRow> b)
    {
        b.OwnsLocalized(r => r.Label);
    }
}

public class CostServiceModelConfig : IEntityTypeConfiguration<CostServiceModel>
{
    public void Configure(EntityTypeBuilder<CostServiceModel> b)
    {
        b.Property(m => m.Name).HasMaxLength(120);
        b.HasMany(m => m.Cells).WithOne().HasForeignKey(c => c.CostServiceModelId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class CostServiceCellConfig : IEntityTypeConfiguration<CostServiceCell>
{
    public void Configure(EntityTypeBuilder<CostServiceCell> b)
    {
        b.Property(c => c.Value).HasMaxLength(100);
    }
}
