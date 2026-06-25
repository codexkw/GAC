using System.ComponentModel.DataAnnotations;

namespace GAC.Core.Content;

public enum VehicleImageKind
{
    // Stored as int (0/1) — display names only relabel the admin UI, no migration.
    // Hero feeds the detail-page banner (_VehicleHero); Gallery feeds the menu/strip
    // thumbnail (UrlHelpers.ThumbPath, which falls back to Hero when none is set).
    [Display(Name = "Banner (detail page)")] Hero = 0,
    [Display(Name = "Menu thumbnail")] Gallery = 1
}

public class VehicleImage
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public VehicleImageKind Kind { get; set; }
    public string Path { get; set; } = "";
    public LocalizedText Alt { get; set; } = new();
    public int SortOrder { get; set; }
}
