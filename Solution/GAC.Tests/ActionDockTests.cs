using GAC.Infrastructure.Data;
using GAC.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GAC.Tests;

public class ActionDockSeedTests
{
    [Fact]
    public async Task GetDockItemsAsync_Returns_Seeded_Visible_Ordered()
    {
        var sp = new ServiceCollection()
            .AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase(nameof(GetDockItemsAsync_Returns_Seeded_Visible_Ordered)))
            .BuildServiceProvider();

        // Seed via the real seeder so we exercise SeedDockItemsAsync.
        await ContentSeeder.SeedAsync(sp);

        var site = new SiteService(sp.GetRequiredService<ApplicationDbContext>());
        var items = await site.GetDockItemsAsync();

        Assert.Equal(6, items.Count);
        Assert.True(items.Select(i => i.SortOrder).SequenceEqual(items.OrderBy(i => i.SortOrder).Select(i => i.SortOrder)));
        Assert.All(items, i => Assert.True(i.IsVisible));
    }
}

public class ActionDockRenderTests : IClassFixture<DevWebApplicationFactory>
{
    private readonly DevWebApplicationFactory _factory;
    public ActionDockRenderTests(DevWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Home_Renders_Dock_Items()
    {
        var html = await (await _factory.CreateClient().GetAsync("/")).Content.ReadAsStringAsync();
        Assert.Contains("action-dock", html);
        Assert.Contains("action-dock__item--wa", html); // WhatsApp item present
    }

    [Fact]
    public async Task Home_Hides_Brochure_When_Not_On_Vehicle_Page()
    {
        var html = await (await _factory.CreateClient().GetAsync("/")).Content.ReadAsStringAsync();
        // The brochure item is VehicleBrochure-typed; off a model page it must not render.
        Assert.DoesNotContain("Download Brochure", html);
    }
}
