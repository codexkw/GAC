using GAC.Core.Content;

namespace GAC.Core.Services;

public interface IAdminHomeService
{
    Task<IReadOnlyList<HeroSlide>> ListSlidesAsync(CancellationToken ct = default);
    Task<HeroSlide?> GetSlideAsync(int id, CancellationToken ct = default);
    Task<int> CreateSlideAsync(HeroSlide slide, CancellationToken ct = default); // attaches to the singleton HomePage (creates it if missing)
    Task<bool> UpdateSlideAsync(HeroSlide slide, CancellationToken ct = default);
    Task<bool> DeleteSlideAsync(int id, CancellationToken ct = default);
    Task<bool> MoveSlideAsync(int id, int direction, CancellationToken ct = default);
}
