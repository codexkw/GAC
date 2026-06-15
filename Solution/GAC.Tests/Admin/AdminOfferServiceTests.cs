using GAC.Core.Content;
using GAC.Infrastructure.Data;
using GAC.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAC.Tests.Admin;

public class AdminOfferServiceTests
{
    private static ApplicationDbContext NewDb(string n) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(n).Options);

    [Fact]
    public async Task Create_Then_Get_RoundTrips()
    {
        var db = NewDb(nameof(Create_Then_Get_RoundTrips));
        var svc = new AdminOfferService(db);
        var id = await svc.CreateAsync(new Offer { Slug = "o1", Title = "O1", IsActive = true });
        var a = await svc.GetAsync(id);
        Assert.NotNull(a);
        Assert.Equal("o1", a!.Slug);
    }

    [Fact]
    public async Task SlugExists_IgnoresSelf()
    {
        var db = NewDb(nameof(SlugExists_IgnoresSelf));
        var svc = new AdminOfferService(db);
        var id = await svc.CreateAsync(new Offer { Slug = "dup", Title = "D" });
        Assert.True(await svc.SlugExistsAsync("dup"));
        Assert.False(await svc.SlugExistsAsync("dup", exceptId: id));
    }

    [Fact]
    public async Task Update_TogglesActive()
    {
        var db = NewDb(nameof(Update_TogglesActive));
        var svc = new AdminOfferService(db);
        var id = await svc.CreateAsync(new Offer { Slug = "o", Title = "T", IsActive = true });
        var a = await svc.GetAsync(id);
        a!.IsActive = false; a.Title = "T2";
        Assert.True(await svc.UpdateAsync(a));
        var r = await db.Offers.FindAsync(id);
        Assert.False(r!.IsActive);
        Assert.Equal("T2", r.Title.En);
    }

    [Fact]
    public async Task Update_ReturnsFalse_WhenMissing()
    {
        var db = NewDb(nameof(Update_ReturnsFalse_WhenMissing));
        var svc = new AdminOfferService(db);
        Assert.False(await svc.UpdateAsync(new Offer { Id = 4242, Slug = "x", Title = "X" }));
    }

    [Fact]
    public async Task Delete_Removes()
    {
        var db = NewDb(nameof(Delete_Removes));
        var svc = new AdminOfferService(db);
        var id = await svc.CreateAsync(new Offer { Slug = "d", Title = "D" });
        Assert.True(await svc.DeleteAsync(id));
        Assert.Null(await db.Offers.FindAsync(id));
    }
}
