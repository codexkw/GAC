using GAC.Core.Content;
using GAC.Infrastructure.Data;
using GAC.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAC.Tests.Admin;

public class AdminDockServiceTests
{
    private static ApplicationDbContext NewDb(string n) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(n).Options);

    [Fact]
    public async Task Create_Update_RoundTrips()
    {
        var db = NewDb(nameof(Create_Update_RoundTrips));
        var svc = new AdminDockService(db);
        var id = await svc.CreateAsync(new DockItem { Label = new() { En = "WhatsApp" }, Icon = "whatsapp", LinkType = DockLinkType.WhatsApp, SortOrder = 0 });

        // Distinct, untracked instance — mirrors MVC model binding handing the service a fresh object.
        var update = new DockItem { Id = id, Label = new() { En = "WhatsApp" }, Icon = "whatsapp", Url = "/x", LinkType = DockLinkType.Url, SortOrder = 0 };
        Assert.True(await svc.UpdateAsync(update));

        var saved = await db.DockItems.FindAsync(id);
        Assert.Equal("/x", saved!.Url);
        Assert.Equal(DockLinkType.Url, saved.LinkType);
    }

    [Fact]
    public async Task Delete_Removes_Item()
    {
        var db = NewDb(nameof(Delete_Removes_Item));
        var svc = new AdminDockService(db);
        var id = await svc.CreateAsync(new DockItem { Label = "A", SortOrder = 0 });
        Assert.True(await svc.DeleteAsync(id));
        Assert.Equal(0, await db.DockItems.CountAsync());
    }

    [Fact]
    public async Task Move_Swaps_SortOrder()
    {
        var db = NewDb(nameof(Move_Swaps_SortOrder));
        var svc = new AdminDockService(db);
        var a = await svc.CreateAsync(new DockItem { Label = "A", SortOrder = 0 });
        var b = await svc.CreateAsync(new DockItem { Label = "B", SortOrder = 1 });
        Assert.True(await svc.MoveAsync(b, -1));
        Assert.Equal(0, (await db.DockItems.FindAsync(b))!.SortOrder);
        Assert.Equal(1, (await db.DockItems.FindAsync(a))!.SortOrder);
    }
}
