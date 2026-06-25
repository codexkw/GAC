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
    Task<bool> UpdateImageAsync(int imageId, string path, VehicleImageKind kind, CancellationToken ct = default);
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
    Task<bool> UpdateColorAsync(int colorId, LocalizedText name, string hex, string? imagePath, CancellationToken ct = default);
    Task<bool> RemoveColorAsync(int colorId, CancellationToken ct = default);
    Task<bool> MoveColorAsync(int colorId, int direction, CancellationToken ct = default);
    // Trims
    Task<int> AddTrimAsync(int vehicleId, Trim trim, CancellationToken ct = default);
    Task<bool> UpdateTrimAsync(Trim trim, CancellationToken ct = default);
    Task<bool> RemoveTrimAsync(int trimId, CancellationToken ct = default);
    Task<bool> MoveTrimAsync(int trimId, int direction, CancellationToken ct = default);

    // Section headings (set-by-key, no reorder)
    Task<int> UpsertSectionHeadingAsync(int vehicleId, SectionKey key, LocalizedText title, LocalizedText sub, LocalizedText body, CancellationToken ct = default);

    // Overview stats
    Task<int> AddStatAsync(int vehicleId, LocalizedText label, LocalizedText value, CancellationToken ct = default);
    Task<bool> RemoveStatAsync(int statId, CancellationToken ct = default);
    Task<bool> MoveStatAsync(int statId, int direction, CancellationToken ct = default);

    // Sliders (group -> slides)
    Task<int> AddSliderAsync(int vehicleId, LocalizedText eyebrow, LocalizedText title, CancellationToken ct = default);
    Task<bool> RemoveSliderAsync(int sliderId, CancellationToken ct = default);
    Task<bool> MoveSliderAsync(int sliderId, int direction, CancellationToken ct = default);
    Task<int> AddSliderSlideAsync(int sliderGroupId, string? imagePath, LocalizedText alt, CancellationToken ct = default);
    Task<bool> UpdateSliderSlideAsync(int slideId, string? imagePath, LocalizedText alt, CancellationToken ct = default);
    Task<bool> RemoveSliderSlideAsync(int slideId, CancellationToken ct = default);
    Task<bool> MoveSliderSlideAsync(int slideId, int direction, CancellationToken ct = default);

    // Feature bullets (feature -> bullets)
    Task<int> AddFeatureBulletAsync(int featureSectionId, LocalizedText label, LocalizedText text, CancellationToken ct = default);
    Task<bool> RemoveFeatureBulletAsync(int bulletId, CancellationToken ct = default);
    Task<bool> MoveFeatureBulletAsync(int bulletId, int direction, CancellationToken ct = default);

    // Gallery tabs (tab -> images)
    Task<int> AddGalleryTabAsync(int vehicleId, LocalizedText label, CancellationToken ct = default);
    Task<bool> RemoveGalleryTabAsync(int tabId, CancellationToken ct = default);
    Task<bool> MoveGalleryTabAsync(int tabId, int direction, CancellationToken ct = default);
    Task<int> AddGalleryImageAsync(int galleryTabId, string? imagePath, LocalizedText alt, CancellationToken ct = default);
    Task<bool> UpdateGalleryImageAsync(int imageId, string? imagePath, LocalizedText alt, CancellationToken ct = default);
    Task<bool> RemoveGalleryImageAsync(int imageId, CancellationToken ct = default);
    Task<bool> MoveGalleryImageAsync(int imageId, int direction, CancellationToken ct = default);

    // Quality block (0/1 per vehicle)
    Task<int> UpsertQualityAsync(int vehicleId, string? mainImage, string? thumbImage, LocalizedText strapline, LocalizedText content, CancellationToken ct = default);
    Task<bool> RemoveQualityAsync(int vehicleId, CancellationToken ct = default);

    // Card items (technology cards)
    Task<int> AddCardAsync(int vehicleId, LocalizedText title, LocalizedText text, string? imagePath, CancellationToken ct = default);
    Task<bool> UpdateCardAsync(int cardId, LocalizedText title, LocalizedText text, string? imagePath, CancellationToken ct = default);
    Task<bool> RemoveCardAsync(int cardId, CancellationToken ct = default);
    Task<bool> MoveCardAsync(int cardId, int direction, CancellationToken ct = default);

    // Safety toggles
    Task<int> AddSafetyToggleAsync(int vehicleId, LocalizedText title, string? imagePath, LocalizedText strap, LocalizedText content, CancellationToken ct = default);
    Task<bool> UpdateSafetyToggleAsync(int toggleId, LocalizedText title, string? imagePath, LocalizedText strap, LocalizedText content, CancellationToken ct = default);
    Task<bool> RemoveSafetyToggleAsync(int toggleId, CancellationToken ct = default);
    Task<bool> MoveSafetyToggleAsync(int toggleId, int direction, CancellationToken ct = default);

    // Trim price rows (grandchild: TrimId-scoped)
    Task<int> AddTrimPriceRowAsync(int trimId, LocalizedText text, CancellationToken ct = default);
    Task<bool> RemoveTrimPriceRowAsync(int rowId, CancellationToken ct = default);
    Task<bool> MoveTrimPriceRowAsync(int rowId, int direction, CancellationToken ct = default);

    // Warranty links
    Task<int> AddWarrantyLinkAsync(int vehicleId, LocalizedText label, string? url, CancellationToken ct = default);
    Task<bool> RemoveWarrantyLinkAsync(int linkId, CancellationToken ct = default);
    Task<bool> MoveWarrantyLinkAsync(int linkId, int direction, CancellationToken ct = default);
}
