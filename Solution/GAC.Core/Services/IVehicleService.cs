using GAC.Core.Content;

namespace GAC.Core.Services;

public interface IVehicleService
{
    Task<IReadOnlyList<Vehicle>> GetVisibleAsync();
    Task<Vehicle?> GetBySlugAsync(string slug);
}
