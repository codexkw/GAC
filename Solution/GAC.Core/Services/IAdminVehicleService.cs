using GAC.Core.Content;

namespace GAC.Core.Services;

public interface IAdminVehicleService
{
    Task<IReadOnlyList<Vehicle>> ListAsync(CancellationToken ct = default);          // incl. hidden, ordered
    Task<Vehicle?> GetAsync(int id, CancellationToken ct = default);                 // +Images
    Task<bool> SlugExistsAsync(string slug, int? exceptId = null, CancellationToken ct = default);
    Task<int> CreateAsync(Vehicle vehicle, CancellationToken ct = default);          // returns new Id
    Task<bool> UpdateAsync(Vehicle vehicle, CancellationToken ct = default);         // scalar + localized fields
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    Task<bool> MoveAsync(int id, int direction, CancellationToken ct = default);     // -1 up, +1 down (swaps SortOrder)
    Task<int> AddImageAsync(int vehicleId, string path, VehicleImageKind kind, CancellationToken ct = default);
    Task<bool> RemoveImageAsync(int imageId, CancellationToken ct = default);
    Task<bool> MoveImageAsync(int imageId, int direction, CancellationToken ct = default);

    // Feature blocks
    Task<FeatureSection?> GetFeatureAsync(int featureId, CancellationToken ct = default);
    Task<int> AddFeatureAsync(int vehicleId, FeatureSection feature, CancellationToken ct = default);
    Task<bool> UpdateFeatureAsync(FeatureSection feature, CancellationToken ct = default);
    Task<bool> RemoveFeatureAsync(int featureId, CancellationToken ct = default);
    Task<bool> MoveFeatureAsync(int featureId, int direction, CancellationToken ct = default);

    // Spec groups + rows
    Task<int> AddSpecGroupAsync(int vehicleId, LocalizedText title, CancellationToken ct = default);
    Task<bool> RemoveSpecGroupAsync(int groupId, CancellationToken ct = default);
    Task<bool> MoveSpecGroupAsync(int groupId, int direction, CancellationToken ct = default);
    Task<int> AddSpecRowAsync(int groupId, LocalizedText label, LocalizedText value, CancellationToken ct = default);
    Task<bool> RemoveSpecRowAsync(int rowId, CancellationToken ct = default);
    // Colours
    Task<int> AddColorAsync(int vehicleId, LocalizedText name, string hex, string? imagePath, CancellationToken ct = default);
    Task<bool> RemoveColorAsync(int colorId, CancellationToken ct = default);
    Task<bool> MoveColorAsync(int colorId, int direction, CancellationToken ct = default);
    // Trims
    Task<int> AddTrimAsync(int vehicleId, Trim trim, CancellationToken ct = default);
    Task<bool> RemoveTrimAsync(int trimId, CancellationToken ct = default);
    Task<bool> MoveTrimAsync(int trimId, int direction, CancellationToken ct = default);

    // Section headings (set-by-key, no reorder)
    Task<int> UpsertSectionHeadingAsync(int vehicleId, SectionKey key, LocalizedText title, LocalizedText sub, LocalizedText body, CancellationToken ct = default);

    // Overview stats
    Task<int> AddStatAsync(int vehicleId, LocalizedText label, LocalizedText value, CancellationToken ct = default);
    Task<bool> RemoveStatAsync(int statId, CancellationToken ct = default);
    Task<bool> MoveStatAsync(int statId, int direction, CancellationToken ct = default);
}
