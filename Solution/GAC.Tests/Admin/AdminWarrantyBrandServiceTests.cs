using System.Linq;
using GAC.Core.Content;
using GAC.Infrastructure.Data;
using GAC.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAC.Tests.Admin;

public class AdminWarrantyBrandServiceTests
{
    private static ApplicationDbContext NewDb(string n) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(n).Options);

    [Fact]
    public async Task Save_PersistsHeaders_AndReplacesBrandRowsWholesale()
    {
        var db = NewDb(nameof(Save_PersistsHeaders_AndReplacesBrandRowsWholesale));
        var svc = new AdminWarrantyService(db);

        await svc.SaveAsync(new WarrantyPage
        {
            TableMfrWarrantyHeader = new LocalizedText { En = "Manufacturer Warranty", Ar = "ضمان المصنّع" },
            BrandRows = { new WarrantyBrandRow { SortOrder = 0, Brand = "OldBrand",
                ManufacturerWarranty = new LocalizedText { En = "x" } } }
        });
        await svc.SaveAsync(new WarrantyPage
        {
            TableMfrWarrantyHeader = new LocalizedText { En = "Manufacturer Warranty", Ar = "ضمان المصنّع" },
            BrandRows = { new WarrantyBrandRow { SortOrder = 0, Brand = "GAC",
                ManufacturerWarranty = new LocalizedText { En = "5 Years" }, PolicyUrl = "/p.pdf" } }
        });

        var p = await svc.GetAsync();
        Assert.Equal("ضمان المصنّع", p.TableMfrWarrantyHeader.Ar);
        Assert.Single(p.BrandRows);
        Assert.Equal("GAC", p.BrandRows[0].Brand);
        Assert.Equal("/p.pdf", p.BrandRows[0].PolicyUrl);
        Assert.Equal(1, await db.WarrantyBrandRows.CountAsync());   // old row not orphaned
    }

    [Fact]
    public async Task Save_DropsBlankRows_AndReindexes()
    {
        var db = NewDb(nameof(Save_DropsBlankRows_AndReindexes));
        var svc = new AdminWarrantyService(db);

        await svc.SaveAsync(new WarrantyPage
        {
            BrandRows =
            {
                new WarrantyBrandRow { SortOrder = 0, Brand = "GAC", ManufacturerWarranty = new LocalizedText { En = "a" } },
                new WarrantyBrandRow { SortOrder = 1, Brand = "", PolicyUrl = "" },           // fully blank → dropped
                new WarrantyBrandRow { SortOrder = 2, Brand = "GMC", ManufacturerWarranty = new LocalizedText { En = "b" } },
            }
        });

        var p = await svc.GetAsync();
        Assert.Equal(new[] { "GAC", "GMC" }, p.BrandRows.Select(r => r.Brand).ToArray());
        Assert.Equal(new[] { 0, 1 }, p.BrandRows.Select(r => r.SortOrder).ToArray());   // reindexed
    }
}
