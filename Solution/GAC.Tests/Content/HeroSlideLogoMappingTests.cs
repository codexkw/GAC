using System.Threading.Tasks;
using GAC.Core.Content;
using GAC.Infrastructure.Data;
using GAC.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAC.Tests.Content;

public class HeroSlideLogoMappingTests
{
    private static ApplicationDbContext NewDb(string n) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(n).Options);

    [Fact]
    public async Task HeroSlide_LogoImagePath_RoundTrips()
    {
        var db = NewDb(nameof(HeroSlide_LogoImagePath_RoundTrips));
        db.HeroSlides.Add(new HeroSlide { ImagePath = "/bg.jpg", LogoImagePath = "/logo/gs8.png",
            Heading = new LocalizedText { En = "GS8" } });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var s = await db.HeroSlides.FirstAsync();
        Assert.Equal("/logo/gs8.png", s.LogoImagePath);
    }

    [Fact]
    public async Task UpdateSlideAsync_Persists_LogoImagePath()
    {
        var db = NewDb(nameof(UpdateSlideAsync_Persists_LogoImagePath));
        var svc = new AdminHomeService(db);
        var id = await svc.CreateSlideAsync(new HeroSlide { ImagePath = "/bg.jpg",
            Heading = new LocalizedText { En = "GS8" } });

        await svc.UpdateSlideAsync(new HeroSlide { Id = id, ImagePath = "/bg.jpg",
            Heading = new LocalizedText { En = "GS8" }, LogoImagePath = "/logo/gs8.png" });

        var s = await db.HeroSlides.FirstAsync(x => x.Id == id);
        Assert.Equal("/logo/gs8.png", s.LogoImagePath);
    }
}
