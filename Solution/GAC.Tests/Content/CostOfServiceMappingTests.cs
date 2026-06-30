using System.Linq;
using GAC.Core.Content;
using GAC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAC.Tests.Content;

public class CostOfServiceMappingTests
{
    private static ApplicationDbContext NewDb(string n) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(n).Options);

    [Fact]
    public async Task CostOfServicePage_WithMatrix_RoundTrips()
    {
        var db = NewDb(nameof(CostOfServicePage_WithMatrix_RoundTrips));
        db.CostOfServicePages.Add(new CostOfServicePage
        {
            Title = new LocalizedText { En = "Cost of Service", Ar = "تكلفة الصيانة" },
            ButtonLabel = new LocalizedText { En = "Spare Parts Policy", Ar = "سياسة قطع الغيار" },
            ButtonUrl = "/docs/policy.pdf",
            TableHeadLabel = new LocalizedText { En = "TYPE/Brand", Ar = "النوع / الماركة" },
            FooterNote = new LocalizedText { En = "line1\nline2", Ar = "س1\nس2" },
            Rows =
            {
                new CostServiceRow { SortOrder = 0, Label = new LocalizedText { En = "5,000 KM", Ar = "5,000 كم" } },
                new CostServiceRow { SortOrder = 1, Label = new LocalizedText { En = "10,000 KM", Ar = "10,000 كم" } },
            },
            Models =
            {
                new CostServiceModel { SortOrder = 0, Name = "M8",
                    Cells = { new CostServiceCell { SortOrder = 0, Value = "525" }, new CostServiceCell { SortOrder = 1, Value = "840" } } },
                new CostServiceModel { SortOrder = 1, Name = "GS4 Max",
                    Cells = { new CostServiceCell { SortOrder = 0, Value = "525" }, new CostServiceCell { SortOrder = 1, Value = "710" } } },
            }
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var p = await db.CostOfServicePages
            .Include(x => x.Rows.OrderBy(r => r.SortOrder))
            .Include(x => x.Models.OrderBy(m => m.SortOrder)).ThenInclude(m => m.Cells.OrderBy(c => c.SortOrder))
            .AsSplitQuery().AsNoTracking().FirstAsync();

        Assert.Equal("تكلفة الصيانة", p.Title.Ar);
        Assert.Equal("/docs/policy.pdf", p.ButtonUrl);
        Assert.Equal(2, p.Rows.Count);
        Assert.Equal(2, p.Models.Count);
        Assert.Equal("GS4 Max", p.Models[1].Name);
        Assert.Equal("710", p.Models[1].Cells[1].Value);   // GS4 Max @ 10,000 KM
        Assert.Equal("10,000 KM", p.Rows[1].Label.En);
    }
}
