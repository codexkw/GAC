using System;
using System.Threading.Tasks;
using GAC.Core.Content;
using GAC.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GAC.Tests.Home;

// Proves a form page renders its editable banner image + intro text FROM THE
// DATABASE, while leaving the form itself intact. Hermetic in-memory DB.
public class FormPageRenderTests : IClassFixture<FormPageRenderTests.Factory>
{
    public class Factory : WebApplicationFactory<Program>
    {
        private readonly string _db = "form-render-" + Guid.NewGuid();
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureTestServices(s => InMemoryTestDb.Swap(s, _db));
        }
    }

    private readonly Factory _factory;
    public FormPageRenderTests(Factory factory) => _factory = factory;

    [Fact]
    public async Task FormPage_RendersBannerAndIntro_FromDatabase_KeepsForm()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var fleet = await db.FormPages.FirstAsync(f => f.Slug == "fleet");
            fleet.BannerImagePath = "/zzz-banner.jpg";
            fleet.IntroText = new LocalizedText { En = "ZZZ-FORM-INTRO", Ar = "ZZZ-FORM-INTRO" };
            await db.SaveChangesAsync();
        }

        var html = await (await _factory.CreateClient().GetAsync("/fleet")).Content.ReadAsStringAsync();

        Assert.Contains("/zzz-banner.jpg", html);     // banner came from the DB
        Assert.Contains("ZZZ-FORM-INTRO", html);      // intro text came from the DB
        Assert.Contains("/forms/fleet", html);        // the lead form itself is still rendered
    }

    [Fact]
    public async Task FleetPage_Intro_RendersAfterImage_BeforeForm_NoHardcodedDuplicate()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var fleet = await db.FormPages.FirstAsync(f => f.Slug == "fleet");
            fleet.BannerImagePath = "/zzz-fleet-banner.jpg";
            fleet.IntroText = new LocalizedText { En = "ZZZ-UNIQUE-FLEET-INTRO", Ar = "ZZZ-UNIQUE-FLEET-INTRO" };
            await db.SaveChangesAsync();
        }

        var html = await (await _factory.CreateClient().GetAsync("/fleet")).Content.ReadAsStringAsync();

        // Anchor on the BODY paragraph markup (class="fleet-intro"), not the raw
        // intro text — the latter also lands in the <head> meta description (SEO),
        // which would otherwise sort before the body banner.
        const string bodyIntro = "fleet-intro\">ZZZ-UNIQUE-FLEET-INTRO";
        var iBanner = html.IndexOf("class=\"fleet-banner\"", StringComparison.Ordinal);
        var iIntro  = html.IndexOf(bodyIntro, StringComparison.Ordinal);
        var iForm   = html.IndexOf("/forms/fleet", StringComparison.Ordinal);

        Assert.True(iBanner >= 0 && iIntro >= 0 && iForm >= 0, "banner, intro and form must all render");
        Assert.True(iBanner < iIntro, "intro must render AFTER the banner image, not above it");
        Assert.True(iIntro < iForm, "intro must render BEFORE the lead form");
        // The old hardcoded marketing paragraph must NOT co-render alongside the editable intro.
        Assert.DoesNotContain("Whether you are looking for a single vehicle", html);
    }
}
