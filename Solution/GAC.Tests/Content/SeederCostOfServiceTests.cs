using System.Linq;
using GAC.Infrastructure.Data;
using GAC.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAC.Tests.Content;

public class SeederCostOfServiceTests
{
    private static ApplicationDbContext NewDb(string n) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(n).Options);

    [Fact]
    public async Task Seed_ParsesMatrix_Bilingual_AndIsIdempotent()
    {
        var db = NewDb(nameof(Seed_ParsesMatrix_Bilingual_AndIsIdempotent));
        await ContentSeeder.SeedCostOfServiceAsync(db);
        await ContentSeeder.SeedCostOfServiceAsync(db);   // idempotent

        Assert.Equal(1, await db.CostOfServicePages.CountAsync());

        var p = await new ContentService(db).GetCostOfServicePageAsync();
        Assert.NotNull(p);
        Assert.Equal(21, p!.Rows.Count);     // 21 service-interval rows
        Assert.Equal(18, p.Models.Count);    // 18 car-model columns
        Assert.All(p.Models, m => Assert.Equal(21, m.Cells.Count));   // each model has a cell per row

        Assert.False(string.IsNullOrWhiteSpace(p.Title.En));
        Assert.False(string.IsNullOrWhiteSpace(p.Title.Ar));          // Arabic from ar/ seed
        Assert.Contains("Kuwaiti Dinars", p.FooterNote.En);
        Assert.Contains("الدينار", p.FooterNote.Ar);

        Assert.Equal("5,000 KM/6 Month", p.Rows[0].Label.En);
        Assert.Contains("أشهر", p.Rows[0].Label.Ar);

        Assert.Equal("M8", p.Models[0].Name);
        Assert.Equal("GS4 Max", p.Models[5].Name);
        Assert.Equal("GN8", p.Models[17].Name);

        Assert.Equal("525", p.Models[5].Cells[0].Value);     // GS4 Max @ 5,000 KM
        Assert.Equal("710", p.Models[5].Cells[1].Value);     // GS4 Max @ 10,000 KM
        Assert.Equal("2,730", p.Models[0].Cells[20].Value);  // M8 @ 200,000 KM
    }
}
