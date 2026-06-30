using GAC.Core.Content;
using GAC.Infrastructure.Data;
using GAC.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAC.Tests.Content;

public class SeederWarrantyTests
{
    private static ApplicationDbContext NewDb(string n) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(n).Options);

    [Fact]
    public async Task SeedWarranty_IsIdempotent_AndSeedsCallouts()
    {
        var db = NewDb(nameof(SeedWarranty_IsIdempotent_AndSeedsCallouts));
        await ContentSeeder.SeedWarrantyAsync(db);
        await ContentSeeder.SeedWarrantyAsync(db);     // second run must not duplicate

        Assert.Equal(1, await db.WarrantyPages.CountAsync());
        Assert.Equal(2, await db.WarrantyCallouts.CountAsync());
        var w = await new ContentService(db).GetWarrantyPageAsync();
        Assert.Equal("Warranty", w!.Heading.En);
        Assert.Equal(2, w.Callouts.Count);
        Assert.False(string.IsNullOrWhiteSpace(w.ExtendedTableHtml.En));   // brand table seeded
    }
}
