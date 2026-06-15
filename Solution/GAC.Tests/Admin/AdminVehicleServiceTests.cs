using GAC.Core.Content;
using GAC.Infrastructure.Data;
using GAC.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAC.Tests.Admin;

public class AdminVehicleServiceTests
{
    private static ApplicationDbContext NewDb(string n) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(n).Options);

    [Fact]
    public async Task Create_Then_Get_RoundTrips()
    {
        var db = NewDb(nameof(Create_Then_Get_RoundTrips));
        var svc = new AdminVehicleService(db);
        var id = await svc.CreateAsync(new Vehicle { Slug = "x1", Name = "X1", SortOrder = 5, IsVisible = true });
        var v = await svc.GetAsync(id);
        Assert.NotNull(v);
        Assert.Equal("x1", v!.Slug);
    }

    [Fact]
    public async Task SlugExists_DetectsDuplicate_IgnoringSelf()
    {
        var db = NewDb(nameof(SlugExists_DetectsDuplicate_IgnoringSelf));
        var svc = new AdminVehicleService(db);
        var id = await svc.CreateAsync(new Vehicle { Slug = "dup", Name = "D" });
        Assert.True(await svc.SlugExistsAsync("dup"));
        Assert.False(await svc.SlugExistsAsync("dup", exceptId: id));
    }

    [Fact]
    public async Task Move_SwapsSortOrder()
    {
        var db = NewDb(nameof(Move_SwapsSortOrder));
        var svc = new AdminVehicleService(db);
        var a = await svc.CreateAsync(new Vehicle { Slug = "a", Name = "A", SortOrder = 1 });
        var b = await svc.CreateAsync(new Vehicle { Slug = "b", Name = "B", SortOrder = 2 });
        Assert.True(await svc.MoveAsync(b, -1));
        Assert.Equal(1, (await db.Vehicles.FindAsync(b))!.SortOrder);
        Assert.Equal(2, (await db.Vehicles.FindAsync(a))!.SortOrder);
    }

    [Fact]
    public async Task AddImage_Then_Remove()
    {
        var db = NewDb(nameof(AddImage_Then_Remove));
        var svc = new AdminVehicleService(db);
        var id = await svc.CreateAsync(new Vehicle { Slug = "img", Name = "I" });
        var imgId = await svc.AddImageAsync(id, "/uploads/a.png", VehicleImageKind.Gallery);
        Assert.Equal(1, await db.VehicleImages.CountAsync());
        Assert.True(await svc.RemoveImageAsync(imgId));
        Assert.Equal(0, await db.VehicleImages.CountAsync());
    }

    [Fact]
    public async Task Delete_Removes()
    {
        var db = NewDb(nameof(Delete_Removes));
        var svc = new AdminVehicleService(db);
        var id = await svc.CreateAsync(new Vehicle { Slug = "del", Name = "D" });
        Assert.True(await svc.DeleteAsync(id));
        Assert.Null(await db.Vehicles.FindAsync(id));
    }
}
