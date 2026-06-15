using GAC.Core.Content;
using GAC.Infrastructure.Data;
using GAC.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAC.Tests.Admin;

public class AdminHomeServiceTests
{
    private static ApplicationDbContext NewDb(string n) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(n).Options);

    [Fact]
    public async Task CreateSlide_AttachesToHomePage()
    {
        var db = NewDb(nameof(CreateSlide_AttachesToHomePage));
        var svc = new AdminHomeService(db);
        var id = await svc.CreateSlideAsync(new HeroSlide { Heading = "Hi", ImagePath = "/x.jpg" });
        Assert.Equal(1, await db.HeroSlides.CountAsync());
        Assert.Equal(1, await db.HomePages.CountAsync());
        Assert.NotEqual(0, (await db.HeroSlides.FindAsync(id))!.HomePageId);
    }

    [Fact]
    public async Task Update_RoundTrips()
    {
        var db = NewDb(nameof(Update_RoundTrips));
        var svc = new AdminHomeService(db);
        var id = await svc.CreateSlideAsync(new HeroSlide { Heading = "Old", ImagePath = "/x.jpg" });
        var s = await svc.GetSlideAsync(id);
        s!.Heading = "New"; s.CtaLink = "/gs8";
        Assert.True(await svc.UpdateSlideAsync(s));
        var reloaded = await db.HeroSlides.FindAsync(id);
        Assert.Equal("New", reloaded!.Heading.En);
        Assert.Equal("/gs8", reloaded.CtaLink);
    }

    [Fact]
    public async Task Move_SwapsSortOrder()
    {
        var db = NewDb(nameof(Move_SwapsSortOrder));
        var svc = new AdminHomeService(db);
        var a = await svc.CreateSlideAsync(new HeroSlide { Heading = "A", ImagePath = "/a.jpg" }); // SortOrder 0
        var b = await svc.CreateSlideAsync(new HeroSlide { Heading = "B", ImagePath = "/b.jpg" }); // SortOrder 1
        Assert.True(await svc.MoveSlideAsync(b, -1));
        Assert.Equal(0, (await db.HeroSlides.FindAsync(b))!.SortOrder);
        Assert.Equal(1, (await db.HeroSlides.FindAsync(a))!.SortOrder);
    }

    [Fact]
    public async Task Delete_Removes()
    {
        var db = NewDb(nameof(Delete_Removes));
        var svc = new AdminHomeService(db);
        var id = await svc.CreateSlideAsync(new HeroSlide { Heading = "A", ImagePath = "/a.jpg" });
        Assert.True(await svc.DeleteSlideAsync(id));
        Assert.Equal(0, await db.HeroSlides.CountAsync());
    }
}
