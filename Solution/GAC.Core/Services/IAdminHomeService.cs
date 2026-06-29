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

    // Home "Latest Offers" promo block + the three "dual" cards (same HomePage aggregate).
    Task<HomePage> GetHomeAggregateAsync(CancellationToken ct = default);
    Task SavePromoAsync(PromoSection promo, CancellationToken ct = default);   // upserts the singleton promo, replaces its campaigns
    Task<bool> SaveCardAsync(DualCard card, CancellationToken ct = default);    // updates an existing card in place
}
