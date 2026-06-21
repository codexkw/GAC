namespace GAC.Core.Content;

public enum DockLinkType { Url = 0, WhatsApp = 1, Phone = 2, VehicleBrochure = 3 }

/// <summary>A single item in the floating action-dock. Order by SortOrder; render only when IsVisible.</summary>
public class DockItem
{
    public int Id { get; set; }
    public LocalizedText Label { get; set; } = new();       // full text (desktop)
    public LocalizedText ShortLabel { get; set; } = new();  // compact text (mobile)
    public string? Url { get; set; }                        // used when LinkType == Url
    public string Icon { get; set; } = "";                  // icon key (see DockIcons)
    public DockLinkType LinkType { get; set; } = DockLinkType.Url;
    public bool IsVisible { get; set; } = true;
    public int SortOrder { get; set; }
}
