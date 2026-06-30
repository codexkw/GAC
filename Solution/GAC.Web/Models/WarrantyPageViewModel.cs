using GAC.Core.Content;

namespace GAC.Web.Models;

public class WarrantyPageViewModel
{
    public WarrantyPage Warranty { get; set; } = new();
    public IReadOnlyList<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
}
