using GAC.Core.Content;
using GAC.Infrastructure.Data;
using GAC.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAC.Tests.Admin;

public class AdminMenuServiceTests
{
    private static ApplicationDbContext NewDb(string n) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(n).Options);

    [Fact]
    public async Task Create_Update_RoundTrips()
    {
        var db = NewDb(nameof(Create_Update_RoundTrips));
        var svc = new AdminMenuService(db);
        var id = await svc.CreateAsync(new MenuItem { Label = "Home", Url = "/", SortOrder = 0 });
        var item = await svc.GetAsync(id);
        item!.Url = "/home";
        Assert.True(await svc.UpdateAsync(item));
        Assert.Equal("/home", (await db.MenuItems.FindAsync(id))!.Url);
    }

    [Fact]
    public async Task Delete_CascadesChildren()
    {
        var db = NewDb(nameof(Delete_CascadesChildren));
        var svc = new AdminMenuService(db);
        var parent = await svc.CreateAsync(new MenuItem { Label = "More", SortOrder = 0 });
        await svc.CreateAsync(new MenuItem { Label = "Fleet", ParentId = parent, SortOrder = 0 });
        Assert.True(await svc.DeleteAsync(parent));
        Assert.Equal(0, await db.MenuItems.CountAsync());
    }

    [Fact]
    public async Task Move_SwapsWithinSiblings()
    {
        var db = NewDb(nameof(Move_SwapsWithinSiblings));
        var svc = new AdminMenuService(db);
        var a = await svc.CreateAsync(new MenuItem { Label = "A", SortOrder = 0 });
        var b = await svc.CreateAsync(new MenuItem { Label = "B", SortOrder = 1 });
        Assert.True(await svc.MoveAsync(b, -1));
        Assert.Equal(0, (await db.MenuItems.FindAsync(b))!.SortOrder);
    }
}
