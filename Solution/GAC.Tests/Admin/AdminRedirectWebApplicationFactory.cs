using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GAC.Core.Content;
using GAC.Core.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace GAC.Tests.Admin;

// Reuses the test-auth setup from AdminWebApplicationFactory but replaces the
// News/Offer admin services with no-op fakes, so redirect-after-save tests can
// exercise the controller + routing without writing to the real database.
public class AdminRedirectWebApplicationFactory : AdminWebApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services =>
        {
            // Registered after the app's services → last registration wins on resolve.
            services.AddScoped<IAdminNewsService, FakeNews>();
            services.AddScoped<IAdminOfferService, FakeOffers>();
            services.AddScoped<IAdminHomeService, FakeHome>();
        });
    }

    private sealed class FakeHome : IAdminHomeService
    {
        public Task<IReadOnlyList<HeroSlide>> ListSlidesAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<HeroSlide>>(new List<HeroSlide>());
        public Task<HeroSlide?> GetSlideAsync(int id, CancellationToken ct = default) => Task.FromResult<HeroSlide?>(null);
        public Task<int> CreateSlideAsync(HeroSlide slide, CancellationToken ct = default) => Task.FromResult(1);
        public Task<bool> UpdateSlideAsync(HeroSlide slide, CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> DeleteSlideAsync(int id, CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> MoveSlideAsync(int id, int direction, CancellationToken ct = default) => Task.FromResult(true);
        public Task<HomePage> GetHomeAggregateAsync(CancellationToken ct = default) => Task.FromResult(new HomePage());
        public Task SavePromoAsync(PromoSection promo, CancellationToken ct = default) => Task.CompletedTask;
        public Task<bool> SaveCardAsync(DualCard card, CancellationToken ct = default) => Task.FromResult(true);
        public Task<int> CreateCardAsync(DualCard card, CancellationToken ct = default) => Task.FromResult(1);
        public Task<bool> DeleteCardAsync(int id, CancellationToken ct = default) => Task.FromResult(true);
    }

    private sealed class FakeNews : IAdminNewsService
    {
        public Task<IReadOnlyList<NewsArticle>> ListAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<NewsArticle>>(new List<NewsArticle>());
        public Task<NewsArticle?> GetAsync(int id, CancellationToken ct = default) => Task.FromResult<NewsArticle?>(null);
        public Task<bool> SlugExistsAsync(string slug, int? exceptId = null, CancellationToken ct = default) => Task.FromResult(false);
        public Task<int> CreateAsync(NewsArticle a, CancellationToken ct = default) => Task.FromResult(1);
        public Task<bool> UpdateAsync(NewsArticle a, CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> DeleteAsync(int id, CancellationToken ct = default) => Task.FromResult(true);
    }

    private sealed class FakeOffers : IAdminOfferService
    {
        public Task<IReadOnlyList<Offer>> ListAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Offer>>(new List<Offer>());
        public Task<Offer?> GetAsync(int id, CancellationToken ct = default) => Task.FromResult<Offer?>(null);
        public Task<bool> SlugExistsAsync(string slug, int? exceptId = null, CancellationToken ct = default) => Task.FromResult(false);
        public Task<int> CreateAsync(Offer a, CancellationToken ct = default) => Task.FromResult(1);
        public Task<bool> UpdateAsync(Offer a, CancellationToken ct = default) => Task.FromResult(true);
        public Task<bool> DeleteAsync(int id, CancellationToken ct = default) => Task.FromResult(true);
    }
}
