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
            .Include(v => v.Features)
            .Include(v => v.SpecGroups).ThenInclude(g => g.Rows)
            .Include(v => v.Colors)
            .Include(v => v.Trims)
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
