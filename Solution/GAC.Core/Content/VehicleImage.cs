namespace GAC.Core.Content;

public enum VehicleImageKind { Hero = 0, Gallery = 1 }

public class VehicleImage
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public VehicleImageKind Kind { get; set; }
    public string Path { get; set; } = "";
    public LocalizedText Alt { get; set; } = new();
    public int SortOrder { get; set; }
}
