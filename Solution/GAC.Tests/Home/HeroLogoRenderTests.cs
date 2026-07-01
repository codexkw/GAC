using System;
using System.Linq;
using System.Threading.Tasks;
using GAC.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GAC.Tests.Home;

public class HeroLogoRenderTests : IClassFixture<HeroLogoRenderTests.Factory>
{
    public class Factory : WebApplicationFactory<Program>
    {
        private readonly string _db = "hero-logo-" + Guid.NewGuid();
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureTestServices(s => InMemoryTestDb.Swap(s, _db));
        }
    }

    private readonly Factory _factory;
    public HeroLogoRenderTests(Factory factory) => _factory = factory;

    [Fact]
    public async Task Hero_RendersLogoWhenSet_ElseHeadingText()
    {
        // Give the first slide a logo, ensure a second slide has none.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var slides = await db.HeroSlides.OrderBy(s => s.SortOrder).ToListAsync();
            slides[0].LogoImagePath = "/media/zzz-logo.png";
            slides[1].LogoImagePath = null;
            await db.SaveChangesAsync();
        }

        var html = await (await _factory.CreateClient().GetAsync("/")).Content.ReadAsStringAsync();

        Assert.Contains("hero__logo", html);                 // logo img class rendered
        Assert.Contains("/media/zzz-logo.png", html);        // the logo src
        Assert.Contains("hero__title", html);                // a no-logo slide still shows the text <h1>
    }
}
