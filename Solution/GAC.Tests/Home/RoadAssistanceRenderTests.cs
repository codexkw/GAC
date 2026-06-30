using System;
using System.Threading.Tasks;
using GAC.Core.Content;
using GAC.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GAC.Tests.Home;

// Proves /road-assistance renders the structured RoadAssistancePage fields FROM THE
// DATABASE (not the old raw-HTML blob), incl. the phone-driven Call button.
public class RoadAssistanceRenderTests : IClassFixture<RoadAssistanceRenderTests.Factory>
{
    public class Factory : WebApplicationFactory<Program>
    {
        private readonly string _db = "roadassist-render-" + Guid.NewGuid();
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureTestServices(s => InMemoryTestDb.Swap(s, _db));
        }
    }

    private readonly Factory _factory;
    public RoadAssistanceRenderTests(Factory factory) => _factory = factory;

    [Fact]
    public async Task RoadAssistance_RendersStructuredFields_FromDatabase()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var r = await db.RoadAssistancePages.FirstAsync();
            r.Heading = new LocalizedText { En = "ZZZ-RA-HEAD", Ar = "ZZZ-RA-HEAD-AR" };
            r.Intro = new LocalizedText { En = "ZZZ-RA-INTRO", Ar = "ZZZ-RA-INTRO" };
            r.PhoneNumber = "1833334";
            r.CallButtonLabel = new LocalizedText { En = "ZZZ-CALL", Ar = "ZZZ-CALL" };
            await db.SaveChangesAsync();
        }

        var html = await (await _factory.CreateClient().GetAsync("/road-assistance")).Content.ReadAsStringAsync();

        Assert.Contains("ZZZ-RA-HEAD", html);          // structured heading from DB
        Assert.Contains("ZZZ-RA-INTRO", html);         // intro paragraph from DB
        Assert.Contains("href=\"tel:1833334\"", html); // phone-driven call button
        Assert.Contains("ZZZ-CALL", html);             // call button label from DB
    }

    [Fact]
    public async Task RoadAssistance_LocalizesHeading_ToArabic()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var r = await db.RoadAssistancePages.FirstAsync();
            r.Heading = new LocalizedText { En = "EN-HEAD", Ar = "ZZZ-AR-HEAD" };
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient();
        var cookie = CookieRequestCultureProvider.MakeCookieValue(new RequestCulture("ar"));
        client.DefaultRequestHeaders.Add("Cookie", $"{CookieRequestCultureProvider.DefaultCookieName}={cookie}");

        var html = await (await client.GetAsync("/road-assistance")).Content.ReadAsStringAsync();

        Assert.Contains("ZZZ-AR-HEAD", html);
    }
}
