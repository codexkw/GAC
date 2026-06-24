using GAC.Core.Content;
using GAC.Core.Services;
using GAC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GAC.Infrastructure.Services;

public class AdminVehicleService : IAdminVehicleService
{
    private readonly ApplicationDbContext _db;
    private readonly IHtmlSanitizerService _sanitizer;
    public AdminVehicleService(ApplicationDbContext db, IHtmlSanitizerService sanitizer)
    { _db = db; _sanitizer = sanitizer; }

    public async Task<IReadOnlyList<Vehicle>> ListAsync(CancellationToken ct = default)
        => await _db.Vehicles.OrderBy(v => v.SortOrder).ToListAsync(ct);

    public async Task<Vehicle?> GetAsync(int id, CancellationToken ct = default)
        => await _db.Vehicles
            .Include(v => v.Images)
            .Include(v => v.Features).ThenInclude(f => f.Bullets)
            .Include(v => v.SpecGroups).ThenInclude(g => g.Rows)
            .Include(v => v.Colors)
            .Include(v => v.Trims).ThenInclude(t => t.PriceRows)
            .Include(v => v.Headings)
            .Include(v => v.Stats)
            .Include(v => v.Sliders).ThenInclude(s => s.Slides)
            .Include(v => v.GalleryTabs).ThenInclude(g => g.Images)
            .Include(v => v.Cards)
            .Include(v => v.SafetyToggles)
            .Include(v => v.WarrantyLinks)
            .Include(v => v.Quality)
            // Split into one query per collection to avoid a cartesian-explosion timeout on
            // content-rich cars (same reason as VehicleService.GetBySlugAsync).
            .AsSplitQuery()
            .FirstOrDefaultAsync(v => v.Id == id, ct);

    public async Task<bool> SlugExistsAsync(string slug, int? exceptId = null, CancellationToken ct = default)
        => await _db.Vehicles.AnyAsync(v => v.Slug == slug && (exceptId == null || v.Id != exceptId), ct);

    public async Task<int> CreateAsync(Vehicle vehicle, CancellationToken ct = default)
    {
        _db.Vehicles.Add(vehicle);
        await _db.SaveChangesAsync(ct);
        return vehicle.Id;
    }

    public async Task<bool> UpdateAsync(Vehicle vehicle, CancellationToken ct = default)
    {
        var existing = await _db.Vehicles.FirstOrDefaultAsync(v => v.Id == vehicle.Id, ct);
        if (existing is null) return false;
        existing.Slug = vehicle.Slug;
        existing.Category = vehicle.Category;
        existing.SortOrder = vehicle.SortOrder;
        existing.IsVisible = vehicle.IsVisible;
        existing.PriceFrom = vehicle.PriceFrom;
        existing.BrochurePdf = vehicle.BrochurePdf;
        existing.SpecPdf = vehicle.SpecPdf;
        existing.Name = vehicle.Name;
        existing.Tagline = vehicle.Tagline;
        existing.IntroText = vehicle.IntroText;
        existing.BodyHtml = vehicle.BodyHtml;
        existing.MetaTitle = vehicle.MetaTitle;
        existing.MetaDescription = vehicle.MetaDescription;
        existing.TechBannerImage = vehicle.TechBannerImage;
        existing.EnquiryBgImage = vehicle.EnquiryBgImage;
        existing.StatsNote = vehicle.StatsNote;
        existing.EnquiryTitle = vehicle.EnquiryTitle;
        existing.EnquirySub = vehicle.EnquirySub;
        existing.EnquiryLead = vehicle.EnquiryLead;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var v = await _db.Vehicles.FindAsync([id], ct);
        if (v is null) return false;
        _db.Vehicles.Remove(v);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> MoveAsync(int id, int direction, CancellationToken ct = default)
    {
        var all = await _db.Vehicles.OrderBy(v => v.SortOrder).ToListAsync(ct);
        var idx = all.FindIndex(v => v.Id == id);
        if (idx < 0) return false;
        var swap = idx + direction;
        if (swap < 0 || swap >= all.Count) return false;
        (all[idx].SortOrder, all[swap].SortOrder) = (all[swap].SortOrder, all[idx].SortOrder);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<int> AddImageAsync(int vehicleId, string path, VehicleImageKind kind, CancellationToken ct = default)
    {
        if (!await _db.Vehicles.AnyAsync(v => v.Id == vehicleId, ct)) return 0;
        var nextOrder = await _db.VehicleImages.Where(i => i.VehicleId == vehicleId).CountAsync(ct);
        var img = new VehicleImage { VehicleId = vehicleId, Path = path, Kind = kind, SortOrder = nextOrder };
        _db.VehicleImages.Add(img);
        await _db.SaveChangesAsync(ct);
        return img.Id;
    }

    public async Task<bool> RemoveImageAsync(int imageId, CancellationToken ct = default)
    {
        var img = await _db.VehicleImages.FindAsync([imageId], ct);
        if (img is null) return false;
        _db.VehicleImages.Remove(img);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> MoveImageAsync(int imageId, int direction, CancellationToken ct = default)
    {
        var img = await _db.VehicleImages.FindAsync([imageId], ct);
        if (img is null) return false;
        var siblings = await _db.VehicleImages
            .Where(i => i.VehicleId == img.VehicleId).OrderBy(i => i.SortOrder).ToListAsync(ct);
        var idx = siblings.FindIndex(i => i.Id == imageId);
        var swap = idx + direction;
        if (swap < 0 || swap >= siblings.Count) return false;
        (siblings[idx].SortOrder, siblings[swap].SortOrder) = (siblings[swap].SortOrder, siblings[idx].SortOrder);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<FeatureSection?> GetFeatureAsync(int featureId, CancellationToken ct = default)
        => await _db.Set<FeatureSection>().FirstOrDefaultAsync(f => f.Id == featureId, ct);

    public async Task<int> AddFeatureAsync(int vehicleId, FeatureSection feature, CancellationToken ct = default)
    {
        if (!await _db.Vehicles.AnyAsync(v => v.Id == vehicleId, ct)) return 0;
        feature.VehicleId = vehicleId;
        feature.Body = Sanitize(feature.Body);
        feature.SortOrder = await _db.Set<FeatureSection>().CountAsync(f => f.VehicleId == vehicleId, ct);
        _db.Set<FeatureSection>().Add(feature);
        await _db.SaveChangesAsync(ct);
        return feature.Id;
    }

    public async Task<bool> UpdateFeatureAsync(FeatureSection feature, CancellationToken ct = default)
    {
        var existing = await _db.Set<FeatureSection>().FirstOrDefaultAsync(f => f.Id == feature.Id, ct);
        if (existing is null) return false;
        existing.Heading = feature.Heading;
        existing.Body = Sanitize(feature.Body);
        existing.ImagePath = feature.ImagePath;
        existing.Layout = feature.Layout;
        existing.GroupKey = feature.GroupKey;
        existing.TabLabel = feature.TabLabel;
        existing.Lead = feature.Lead;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> RemoveFeatureAsync(int featureId, CancellationToken ct = default)
    {
        var f = await _db.Set<FeatureSection>().FindAsync([featureId], ct);
        if (f is null) return false;
        _db.Set<FeatureSection>().Remove(f);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> MoveFeatureAsync(int featureId, int direction, CancellationToken ct = default)
    {
        var f = await _db.Set<FeatureSection>().FindAsync([featureId], ct);
        if (f is null) return false;
        var siblings = await _db.Set<FeatureSection>()
            .Where(x => x.VehicleId == f.VehicleId).OrderBy(x => x.SortOrder).ToListAsync(ct);
        var idx = siblings.FindIndex(x => x.Id == featureId);
        var swap = idx + direction;
        if (swap < 0 || swap >= siblings.Count) return false;
        (siblings[idx].SortOrder, siblings[swap].SortOrder) = (siblings[swap].SortOrder, siblings[idx].SortOrder);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    // ---- Spec groups ----
    public async Task<int> AddSpecGroupAsync(int vehicleId, LocalizedText title, CancellationToken ct = default)
    {
        if (!await _db.Vehicles.AnyAsync(v => v.Id == vehicleId, ct)) return 0;
        var g = new SpecGroup
        {
            VehicleId = vehicleId, Title = title,
            SortOrder = await _db.Set<SpecGroup>().CountAsync(x => x.VehicleId == vehicleId, ct)
        };
        _db.Set<SpecGroup>().Add(g);
        await _db.SaveChangesAsync(ct);
        return g.Id;
    }

    public async Task<bool> RemoveSpecGroupAsync(int groupId, CancellationToken ct = default)
        => await RemoveByIdAsync<SpecGroup>(groupId, ct);

    public async Task<bool> MoveSpecGroupAsync(int groupId, int direction, CancellationToken ct = default)
    {
        var g = await _db.Set<SpecGroup>().FindAsync([groupId], ct);
        if (g is null) return false;
        return await SwapOrderAsync<SpecGroup>(x => x.VehicleId == g.VehicleId, groupId, direction, ct);
    }

    public async Task<int> AddSpecRowAsync(int groupId, LocalizedText label, LocalizedText value, CancellationToken ct = default)
    {
        if (!await _db.Set<SpecGroup>().AnyAsync(g => g.Id == groupId, ct)) return 0;
        var r = new SpecRow
        {
            SpecGroupId = groupId, Label = label, Value = value,
            SortOrder = await _db.Set<SpecRow>().CountAsync(x => x.SpecGroupId == groupId, ct)
        };
        _db.Set<SpecRow>().Add(r);
        await _db.SaveChangesAsync(ct);
        return r.Id;
    }

    public async Task<bool> RemoveSpecRowAsync(int rowId, CancellationToken ct = default)
        => await RemoveByIdAsync<SpecRow>(rowId, ct);

    // ---- Colours ----
    public async Task<int> AddColorAsync(int vehicleId, LocalizedText name, string hex, string? imagePath, CancellationToken ct = default)
    {
        if (!await _db.Vehicles.AnyAsync(v => v.Id == vehicleId, ct)) return 0;
        var c = new ColorOption
        {
            VehicleId = vehicleId, Name = name, Hex = string.IsNullOrWhiteSpace(hex) ? "#000000" : hex,
            ImagePath = imagePath,
            SortOrder = await _db.Set<ColorOption>().CountAsync(x => x.VehicleId == vehicleId, ct)
        };
        _db.Set<ColorOption>().Add(c);
        await _db.SaveChangesAsync(ct);
        return c.Id;
    }

    public async Task<bool> RemoveColorAsync(int colorId, CancellationToken ct = default)
        => await RemoveByIdAsync<ColorOption>(colorId, ct);

    public async Task<bool> MoveColorAsync(int colorId, int direction, CancellationToken ct = default)
    {
        var c = await _db.Set<ColorOption>().FindAsync([colorId], ct);
        if (c is null) return false;
        return await SwapOrderAsync<ColorOption>(x => x.VehicleId == c.VehicleId, colorId, direction, ct);
    }

    // ---- Trims ----
    public async Task<int> AddTrimAsync(int vehicleId, Trim trim, CancellationToken ct = default)
    {
        if (!await _db.Vehicles.AnyAsync(v => v.Id == vehicleId, ct)) return 0;
        trim.VehicleId = vehicleId;
        trim.Highlights = Sanitize(trim.Highlights); // rendered via @Html.Raw — keep consistent with Feature.Body
        trim.SortOrder = await _db.Set<Trim>().CountAsync(x => x.VehicleId == vehicleId, ct);
        _db.Set<Trim>().Add(trim);
        await _db.SaveChangesAsync(ct);
        return trim.Id;
    }

    public async Task<bool> RemoveTrimAsync(int trimId, CancellationToken ct = default)
        => await RemoveByIdAsync<Trim>(trimId, ct);

    public async Task<bool> MoveTrimAsync(int trimId, int direction, CancellationToken ct = default)
    {
        var t = await _db.Set<Trim>().FindAsync([trimId], ct);
        if (t is null) return false;
        return await SwapOrderAsync<Trim>(x => x.VehicleId == t.VehicleId, trimId, direction, ct);
    }

    // ---- Gallery tabs ----
    public async Task<int> AddGalleryTabAsync(int vehicleId, LocalizedText label, CancellationToken ct = default)
    {
        if (!await _db.Vehicles.AnyAsync(v => v.Id == vehicleId, ct)) return 0;
        var e = new GalleryTab { VehicleId = vehicleId, Label = label, SortOrder = await _db.Set<GalleryTab>().CountAsync(x => x.VehicleId == vehicleId, ct) };
        _db.Set<GalleryTab>().Add(e);
        await _db.SaveChangesAsync(ct);
        return e.Id;
    }

    public async Task<bool> RemoveGalleryTabAsync(int tabId, CancellationToken ct = default)
        => await RemoveByIdAsync<GalleryTab>(tabId, ct);

    public async Task<bool> MoveGalleryTabAsync(int tabId, int direction, CancellationToken ct = default)
    {
        var e = await _db.Set<GalleryTab>().FindAsync([tabId], ct);
        if (e is null) return false;
        return await SwapOrderAsync<GalleryTab>(x => x.VehicleId == e.VehicleId, tabId, direction, ct);
    }

    public async Task<int> AddGalleryImageAsync(int galleryTabId, string? imagePath, LocalizedText alt, CancellationToken ct = default)
    {
        if (!await _db.Set<GalleryTab>().AnyAsync(g => g.Id == galleryTabId, ct)) return 0;
        var e = new GalleryImage { GalleryTabId = galleryTabId, ImagePath = imagePath, Alt = alt, SortOrder = await _db.Set<GalleryImage>().CountAsync(x => x.GalleryTabId == galleryTabId, ct) };
        _db.Set<GalleryImage>().Add(e);
        await _db.SaveChangesAsync(ct);
        return e.Id;
    }

    public async Task<bool> RemoveGalleryImageAsync(int imageId, CancellationToken ct = default)
        => await RemoveByIdAsync<GalleryImage>(imageId, ct);

    public async Task<bool> MoveGalleryImageAsync(int imageId, int direction, CancellationToken ct = default)
    {
        var e = await _db.Set<GalleryImage>().FindAsync([imageId], ct);
        if (e is null) return false;
        return await SwapOrderAsync<GalleryImage>(x => x.GalleryTabId == e.GalleryTabId, imageId, direction, ct);
    }

    // ---- Feature bullets ----
    public async Task<int> AddFeatureBulletAsync(int featureSectionId, LocalizedText label, LocalizedText text, CancellationToken ct = default)
    {
        if (!await _db.Set<FeatureSection>().AnyAsync(f => f.Id == featureSectionId, ct)) return 0;
        var e = new FeatureBullet { FeatureSectionId = featureSectionId, Label = label, Text = text, SortOrder = await _db.Set<FeatureBullet>().CountAsync(x => x.FeatureSectionId == featureSectionId, ct) };
        _db.Set<FeatureBullet>().Add(e);
        await _db.SaveChangesAsync(ct);
        return e.Id;
    }

    public async Task<bool> RemoveFeatureBulletAsync(int bulletId, CancellationToken ct = default)
        => await RemoveByIdAsync<FeatureBullet>(bulletId, ct);

    public async Task<bool> MoveFeatureBulletAsync(int bulletId, int direction, CancellationToken ct = default)
    {
        var e = await _db.Set<FeatureBullet>().FindAsync([bulletId], ct);
        if (e is null) return false;
        return await SwapOrderAsync<FeatureBullet>(x => x.FeatureSectionId == e.FeatureSectionId, bulletId, direction, ct);
    }

    // ---- Sliders ----
    public async Task<int> AddSliderAsync(int vehicleId, LocalizedText eyebrow, LocalizedText title, CancellationToken ct = default)
    {
        if (!await _db.Vehicles.AnyAsync(v => v.Id == vehicleId, ct)) return 0;
        var e = new SliderGroup { VehicleId = vehicleId, Eyebrow = eyebrow, Title = title, SortOrder = await _db.Set<SliderGroup>().CountAsync(x => x.VehicleId == vehicleId, ct) };
        _db.Set<SliderGroup>().Add(e);
        await _db.SaveChangesAsync(ct);
        return e.Id;
    }

    public async Task<bool> RemoveSliderAsync(int sliderId, CancellationToken ct = default)
        => await RemoveByIdAsync<SliderGroup>(sliderId, ct);

    public async Task<bool> MoveSliderAsync(int sliderId, int direction, CancellationToken ct = default)
    {
        var e = await _db.Set<SliderGroup>().FindAsync([sliderId], ct);
        if (e is null) return false;
        return await SwapOrderAsync<SliderGroup>(x => x.VehicleId == e.VehicleId, sliderId, direction, ct);
    }

    public async Task<int> AddSliderSlideAsync(int sliderGroupId, string? imagePath, LocalizedText alt, CancellationToken ct = default)
    {
        if (!await _db.Set<SliderGroup>().AnyAsync(g => g.Id == sliderGroupId, ct)) return 0;
        var e = new SliderSlide { SliderGroupId = sliderGroupId, ImagePath = imagePath, Alt = alt, SortOrder = await _db.Set<SliderSlide>().CountAsync(x => x.SliderGroupId == sliderGroupId, ct) };
        _db.Set<SliderSlide>().Add(e);
        await _db.SaveChangesAsync(ct);
        return e.Id;
    }

    public async Task<bool> RemoveSliderSlideAsync(int slideId, CancellationToken ct = default)
        => await RemoveByIdAsync<SliderSlide>(slideId, ct);

    public async Task<bool> MoveSliderSlideAsync(int slideId, int direction, CancellationToken ct = default)
    {
        var e = await _db.Set<SliderSlide>().FindAsync([slideId], ct);
        if (e is null) return false;
        return await SwapOrderAsync<SliderSlide>(x => x.SliderGroupId == e.SliderGroupId, slideId, direction, ct);
    }

    // ---- Overview stats ----
    public async Task<int> AddStatAsync(int vehicleId, LocalizedText label, LocalizedText value, CancellationToken ct = default)
    {
        if (!await _db.Vehicles.AnyAsync(v => v.Id == vehicleId, ct)) return 0;
        var e = new StatItem { VehicleId = vehicleId, Label = label, Value = value, SortOrder = await _db.Set<StatItem>().CountAsync(x => x.VehicleId == vehicleId, ct) };
        _db.Set<StatItem>().Add(e);
        await _db.SaveChangesAsync(ct);
        return e.Id;
    }

    public async Task<bool> RemoveStatAsync(int statId, CancellationToken ct = default)
        => await RemoveByIdAsync<StatItem>(statId, ct);

    public async Task<bool> MoveStatAsync(int statId, int direction, CancellationToken ct = default)
    {
        var e = await _db.Set<StatItem>().FindAsync([statId], ct);
        if (e is null) return false;
        return await SwapOrderAsync<StatItem>(x => x.VehicleId == e.VehicleId, statId, direction, ct);
    }

    // ---- Section headings ----
    public async Task<int> UpsertSectionHeadingAsync(int vehicleId, SectionKey key, LocalizedText title, LocalizedText sub, LocalizedText body, CancellationToken ct = default)
    {
        if (!await _db.Vehicles.AnyAsync(v => v.Id == vehicleId, ct)) return 0;
        var existing = await _db.Set<SectionHeading>().FirstOrDefaultAsync(h => h.VehicleId == vehicleId && h.Key == key, ct);
        if (existing is null)
        {
            existing = new SectionHeading { VehicleId = vehicleId, Key = key };
            _db.Set<SectionHeading>().Add(existing);
        }
        existing.Title = title;
        existing.Sub = sub;
        existing.Body = body;
        await _db.SaveChangesAsync(ct);
        return existing.Id;
    }

    // ---- Quality block ----
    public async Task<int> UpsertQualityAsync(int vehicleId, string? mainImage, string? thumbImage, LocalizedText strapline, LocalizedText content, CancellationToken ct = default)
    {
        if (!await _db.Vehicles.AnyAsync(v => v.Id == vehicleId, ct)) return 0;
        var existing = await _db.QualityBlocks.FirstOrDefaultAsync(q => q.VehicleId == vehicleId, ct);
        if (existing is null)
        {
            existing = new QualityBlock { VehicleId = vehicleId };
            _db.QualityBlocks.Add(existing);
        }
        existing.MainImage = mainImage;
        existing.ThumbImage = thumbImage;
        existing.Strapline = strapline;
        existing.Content = content;
        await _db.SaveChangesAsync(ct);
        return existing.Id;
    }

    public async Task<bool> RemoveQualityAsync(int vehicleId, CancellationToken ct = default)
    {
        var existing = await _db.QualityBlocks.FirstOrDefaultAsync(q => q.VehicleId == vehicleId, ct);
        if (existing is null) return false;
        _db.QualityBlocks.Remove(existing);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    // ---- Card items ----
    public async Task<int> AddCardAsync(int vehicleId, LocalizedText title, LocalizedText text, string? imagePath, CancellationToken ct = default)
    {
        if (!await _db.Vehicles.AnyAsync(v => v.Id == vehicleId, ct)) return 0;
        var e = new CardItem
        {
            VehicleId = vehicleId, Title = title, Text = text, ImagePath = imagePath,
            SortOrder = await _db.CardItems.CountAsync(x => x.VehicleId == vehicleId, ct)
        };
        _db.CardItems.Add(e);
        await _db.SaveChangesAsync(ct);
        return e.Id;
    }

    public async Task<bool> RemoveCardAsync(int cardId, CancellationToken ct = default)
        => await RemoveByIdAsync<CardItem>(cardId, ct);

    public async Task<bool> MoveCardAsync(int cardId, int direction, CancellationToken ct = default)
    {
        var e = await _db.CardItems.FindAsync([cardId], ct);
        if (e is null) return false;
        return await SwapOrderAsync<CardItem>(x => x.VehicleId == e.VehicleId, cardId, direction, ct);
    }

    // ---- Safety toggles ----
    public async Task<int> AddSafetyToggleAsync(int vehicleId, LocalizedText title, string? imagePath, LocalizedText strap, LocalizedText content, CancellationToken ct = default)
    {
        if (!await _db.Vehicles.AnyAsync(v => v.Id == vehicleId, ct)) return 0;
        var e = new SafetyToggle
        {
            VehicleId = vehicleId, Title = title, ImagePath = imagePath, Strap = strap, Content = content,
            SortOrder = await _db.SafetyToggles.CountAsync(x => x.VehicleId == vehicleId, ct)
        };
        _db.SafetyToggles.Add(e);
        await _db.SaveChangesAsync(ct);
        return e.Id;
    }

    public async Task<bool> RemoveSafetyToggleAsync(int toggleId, CancellationToken ct = default)
        => await RemoveByIdAsync<SafetyToggle>(toggleId, ct);

    public async Task<bool> MoveSafetyToggleAsync(int toggleId, int direction, CancellationToken ct = default)
    {
        var e = await _db.SafetyToggles.FindAsync([toggleId], ct);
        if (e is null) return false;
        return await SwapOrderAsync<SafetyToggle>(x => x.VehicleId == e.VehicleId, toggleId, direction, ct);
    }

    // ---- Trim price rows ----
    public async Task<int> AddTrimPriceRowAsync(int trimId, LocalizedText text, CancellationToken ct = default)
    {
        if (!await _db.Set<Trim>().AnyAsync(t => t.Id == trimId, ct)) return 0;
        var e = new TrimPriceRow
        {
            TrimId = trimId, Text = text,
            SortOrder = await _db.TrimPriceRows.CountAsync(x => x.TrimId == trimId, ct)
        };
        _db.TrimPriceRows.Add(e);
        await _db.SaveChangesAsync(ct);
        return e.Id;
    }

    public async Task<bool> RemoveTrimPriceRowAsync(int rowId, CancellationToken ct = default)
        => await RemoveByIdAsync<TrimPriceRow>(rowId, ct);

    public async Task<bool> MoveTrimPriceRowAsync(int rowId, int direction, CancellationToken ct = default)
    {
        var e = await _db.TrimPriceRows.FindAsync([rowId], ct);
        if (e is null) return false;
        return await SwapOrderAsync<TrimPriceRow>(x => x.TrimId == e.TrimId, rowId, direction, ct);
    }

    // ---- Warranty links ----
    public async Task<int> AddWarrantyLinkAsync(int vehicleId, LocalizedText label, string? url, CancellationToken ct = default)
    {
        if (!await _db.Vehicles.AnyAsync(v => v.Id == vehicleId, ct)) return 0;
        var e = new WarrantyLink
        {
            VehicleId = vehicleId, Label = label, Url = url ?? "",
            SortOrder = await _db.WarrantyLinks.CountAsync(x => x.VehicleId == vehicleId, ct)
        };
        _db.WarrantyLinks.Add(e);
        await _db.SaveChangesAsync(ct);
        return e.Id;
    }

    public async Task<bool> RemoveWarrantyLinkAsync(int linkId, CancellationToken ct = default)
        => await RemoveByIdAsync<WarrantyLink>(linkId, ct);

    public async Task<bool> MoveWarrantyLinkAsync(int linkId, int direction, CancellationToken ct = default)
    {
        var e = await _db.WarrantyLinks.FindAsync([linkId], ct);
        if (e is null) return false;
        return await SwapOrderAsync<WarrantyLink>(x => x.VehicleId == e.VehicleId, linkId, direction, ct);
    }

    // ---- shared helpers ----
    private async Task<bool> RemoveByIdAsync<T>(int id, CancellationToken ct) where T : class
    {
        var e = await _db.Set<T>().FindAsync([id], ct);
        if (e is null) return false;
        _db.Set<T>().Remove(e);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    private async Task<bool> SwapOrderAsync<T>(
        System.Linq.Expressions.Expression<System.Func<T, bool>> sibling, int id, int direction, CancellationToken ct)
        where T : class, IOrderable
    {
        var list = await _db.Set<T>().Where(sibling).OrderBy(x => x.SortOrder).ToListAsync(ct);
        var idx = list.FindIndex(x => x.Id == id);
        var swap = idx + direction;
        if (idx < 0 || swap < 0 || swap >= list.Count) return false;
        (list[idx].SortOrder, list[swap].SortOrder) = (list[swap].SortOrder, list[idx].SortOrder);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    private LocalizedText Sanitize(LocalizedText body)
        => new() { En = _sanitizer.Sanitize(body.En), Ar = _sanitizer.Sanitize(body.Ar) };
}
