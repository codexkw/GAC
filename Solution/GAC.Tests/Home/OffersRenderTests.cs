using System;
using System.Linq;
using System.Threading.Tasks;
using GAC.Core.Content;
using GAC.Infrastructure.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GAC.Tests.Home;

// Proves /offers renders its cards FROM THE DATABASE (admin-managed), not from the
// old hardcoded markup, and that the static chrome is localized to Arabic.
public class OffersRenderTests : IClassFixture<OffersRenderTests.Factory>
{
    public class Factory : WebApplicationFactory<Program>
    {
        private readonly string _db = "offers-render-" + Guid.NewGuid();
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureTestServices(s => InMemoryTestDb.Swap(s, _db));
        }
    }

    private readonly Factory _factory;
    public OffersRenderTests(Factory factory) => _factory = factory;

    [Fact]
    public async Task Offers_RenderCardsFromDatabase_NotHardcoded()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Offers.RemoveRange(db.Offers);
            db.Offers.Add(new Offer
            {
                Slug = "zzz-a", SortOrder = 1, IsActive = true,
                Title = new LocalizedText { En = "ZZZ-OFFER-A", Ar = "ZZZ-OFFER-A" },
                Body = new LocalizedText { En = "body a", Ar = "body a" },
                ButtonLabel = new LocalizedText { En = "ZZZ-BTN-A", Ar = "ZZZ-BTN-A" }
            });
            db.Offers.Add(new Offer
            {
                Slug = "zzz-hidden", SortOrder = 2, IsActive = false,
                Title = new LocalizedText { En = "ZZZ-HIDDEN", Ar = "ZZZ-HIDDEN" }
            });
            await db.SaveChangesAsync();
        }

        var html = await (await _factory.CreateClient().GetAsync("/offers")).Content.ReadAsStringAsync();

        Assert.Contains("ZZZ-OFFER-A", html);              // active offer title from DB
        Assert.Contains("ZZZ-BTN-A", html);                // per-offer button label from DB
        Assert.DoesNotContain("ZZZ-HIDDEN", html);         // inactive offer not shown
        Assert.DoesNotContain("offer-card__badge", html);  // old hardcoded badge markup gone
    }

    [Fact]
    public async Task Offers_Chrome_LocalizesToArabic()
    {
        var client = _factory.CreateClient();
        var cookie = CookieRequestCultureProvider.MakeCookieValue(new RequestCulture("ar"));
        client.DefaultRequestHeaders.Add("Cookie", $"{CookieRequestCultureProvider.DefaultCookieName}={cookie}");

        var html = await (await client.GetAsync("/offers")).Content.ReadAsStringAsync();

        Assert.Contains("أحدث العروض", html);   // "Latest Offers" heading localized
    }
}
