using GAC.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GAC.Tests;

public class ServiceTests : IClassFixture<DevWebApplicationFactory>
{
    private readonly DevWebApplicationFactory _factory;
    public ServiceTests(DevWebApplicationFactory factory) => _factory = factory;
    private T Resolve<T>(IServiceScope s) where T : notnull => s.ServiceProvider.GetRequiredService<T>();

    [Fact]
    public async Task GetVisibleAsync_ExcludesHiddenVehicles()
    {
        using var scope = _factory.Services.CreateScope();
        var vehicles = await Resolve<IVehicleService>(scope).GetVisibleAsync();
        Assert.DoesNotContain(vehicles, v => v.Slug == "aion-v");
        Assert.Contains(vehicles, v => v.Slug == "gs8");
        Assert.Equal(vehicles.Select(v => v.SortOrder), vehicles.OrderBy(v => v.SortOrder).Select(v => v.SortOrder));
    }

    [Fact]
    public async Task GetBySlugAsync_ReturnsNull_ForHiddenVehicle()
    {
        using var scope = _factory.Services.CreateScope();
        Assert.Null(await Resolve<IVehicleService>(scope).GetBySlugAsync("aion-v"));
    }

    [Fact]
    public async Task GetContentPageBySlugAsync_ReturnsAbout()
    {
        using var scope = _factory.Services.CreateScope();
        Assert.NotNull(await Resolve<IContentService>(scope).GetContentPageBySlugAsync("about"));
    }

    [Fact]
    public async Task GetMenuAsync_ReturnsTopLevelOrdered()
    {
        using var scope = _factory.Services.CreateScope();
        var menu = await Resolve<ISiteService>(scope).GetMenuAsync();
        Assert.All(menu, m => Assert.Null(m.ParentId));
        Assert.True(menu.Count >= 5);
        Assert.Equal(menu.Select(m => m.SortOrder), menu.OrderBy(m => m.SortOrder).Select(m => m.SortOrder));
    }
}
