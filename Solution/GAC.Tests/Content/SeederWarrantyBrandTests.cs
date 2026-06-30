using System.Linq;
using GAC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAC.Tests.Content;

public class SeederWarrantyBrandTests
{
    private static ApplicationDbContext NewDb(string n) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(n).Options);

    [Fact]
    public async Task Seed_ParsesBrandTable_FromExistingPage_AndIsIdempotent()
    {
        var db = NewDb(nameof(Seed_ParsesBrandTable_FromExistingPage_AndIsIdempotent));
        await ContentSeeder.SeedWarrantyAsync(db);            // creates the page with ExtendedTableHtml
        await ContentSeeder.SeedWarrantyBrandRowsAsync(db);
        await ContentSeeder.SeedWarrantyBrandRowsAsync(db);   // idempotent — no duplicates

        Assert.Equal(1, await db.WarrantyPages.CountAsync());

        var p = await db.WarrantyPages
            .Include(w => w.BrandRows.OrderBy(r => r.SortOrder))
            .AsSplitQuery().AsNoTracking().FirstAsync();

        Assert.Equal(4, p.BrandRows.Count);
        Assert.Equal("GAC", p.BrandRows[0].Brand);
        Assert.Equal("Chevrolet", p.BrandRows[1].Brand);
        Assert.Equal("GMC", p.BrandRows[2].Brand);
        Assert.Equal("Cadillac", p.BrandRows[3].Brand);
        Assert.Equal("5 Years and/or 150,000 KM", p.BrandRows[0].ManufacturerWarranty.En);
        Assert.Contains("Unlimited Mileage", p.BrandRows[0].ExtendedWarranty.En);
        Assert.Contains("\n", p.BrandRows[0].ExtendedWarranty.En);            // <br> preserved as newline
        Assert.Equal("Manufacturer Warranty", p.TableMfrWarrantyHeader.En);   // header EN from <th>
        Assert.False(string.IsNullOrWhiteSpace(p.TableMfrWarrantyHeader.Ar)); // header AR backfilled
        Assert.True(p.BrandRows.All(r => r.PolicyUrl == null));               // '#' links not imported
    }
}
