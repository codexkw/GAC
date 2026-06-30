using GAC.Infrastructure.Data;
using GAC.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAC.Tests.Content;

public class SeederRoadAssistanceTests
{
    private static ApplicationDbContext NewDb(string n) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(n).Options);

    [Fact]
    public async Task Seed_IsIdempotent_AndBilingual()
    {
        var db = NewDb(nameof(Seed_IsIdempotent_AndBilingual));
        await ContentSeeder.SeedRoadAssistanceAsync(db);
        await ContentSeeder.SeedRoadAssistanceAsync(db);   // second run must not duplicate

        Assert.Equal(1, await db.RoadAssistancePages.CountAsync());
        var r = await new ContentService(db).GetRoadAssistancePageAsync();
        Assert.NotNull(r);
        Assert.Equal("Roadside Assistance", r!.Heading.En);
        Assert.False(string.IsNullOrWhiteSpace(r.Heading.Ar));
        Assert.Equal("1833334", r.PhoneNumber);
        Assert.False(string.IsNullOrWhiteSpace(r.CallButtonLabel.En));
    }
}
