using System.Linq;
using GAC.Core.Content;
using GAC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAC.Tests.Content;

public class WarrantyBrandMappingTests
{
    private static ApplicationDbContext NewDb(string n) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(n).Options);

    [Fact]
    public async Task WarrantyPage_WithBrandRows_RoundTrips()
    {
        var db = NewDb(nameof(WarrantyPage_WithBrandRows_RoundTrips));
        db.WarrantyPages.Add(new WarrantyPage
        {
            TableBrandHeader = new LocalizedText { En = "Brand", Ar = "العلامة التجارية" },
            TableMfrWarrantyHeader = new LocalizedText { En = "Manufacturer Warranty", Ar = "ضمان المصنّع" },
            TableMfrRoadsideHeader = new LocalizedText { En = "Mfr Roadside", Ar = "مساعدة المصنّع" },
            TableExtWarrantyHeader = new LocalizedText { En = "Extended Warranty", Ar = "الضمان الممتد" },
            TableExtRoadsideHeader = new LocalizedText { En = "Ext Roadside", Ar = "المساعدة الممتدة" },
            TablePolicyHeader = new LocalizedText { En = "Policy", Ar = "الوثيقة" },
            BrandRows =
            {
                new WarrantyBrandRow { SortOrder = 0, Brand = "GAC",
                    ManufacturerWarranty = new LocalizedText { En = "5 Years and/or 150,000 KM" },
                    ExtendedWarranty = new LocalizedText { En = "+2 Years\n+Unlimited Mileage" },
                    PolicyUrl = "/pdfs/gac.pdf" },
                new WarrantyBrandRow { SortOrder = 1, Brand = "Chevrolet",
                    ManufacturerWarranty = new LocalizedText { En = "3 Years and/or 100,000 KM" } },
            }
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var p = await db.WarrantyPages
            .Include(w => w.BrandRows.OrderBy(r => r.SortOrder))
            .AsSplitQuery().AsNoTracking().FirstAsync();

        Assert.Equal("ضمان المصنّع", p.TableMfrWarrantyHeader.Ar);
        Assert.Equal(2, p.BrandRows.Count);
        Assert.Equal("GAC", p.BrandRows[0].Brand);
        Assert.Equal("/pdfs/gac.pdf", p.BrandRows[0].PolicyUrl);
        Assert.Equal("+2 Years\n+Unlimited Mileage", p.BrandRows[0].ExtendedWarranty.En);  // multi-line preserved
        Assert.Equal("Chevrolet", p.BrandRows[1].Brand);
        Assert.Null(p.BrandRows[1].PolicyUrl);
    }
}
