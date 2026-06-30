using System.Linq;
using GAC.Core.Content;
using GAC.Infrastructure.Data;
using GAC.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAC.Tests.Admin;

public class AdminCostOfServiceServiceTests
{
    private static ApplicationDbContext NewDb(string n) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(n).Options);

    [Fact]
    public async Task Get_OnEmptyDb_CreatesSingleton()
    {
        var db = NewDb(nameof(Get_OnEmptyDb_CreatesSingleton));
        var svc = new AdminCostOfServiceService(db);
        var p = await svc.GetAsync();
        Assert.NotNull(p);
        Assert.Equal(1, await db.CostOfServicePages.CountAsync());
    }

    [Fact]
    public async Task Save_Upserts_ReplacesMatrix_Wholesale()
    {
        var db = NewDb(nameof(Save_Upserts_ReplacesMatrix_Wholesale));
        var svc = new AdminCostOfServiceService(db);

        await svc.SaveAsync(new CostOfServicePage
        {
            Title = "V1",
            Rows = { new CostServiceRow { SortOrder = 0, Label = new LocalizedText { En = "old" } } },
            Models = { new CostServiceModel { SortOrder = 0, Name = "OldCar", Cells = { new CostServiceCell { SortOrder = 0, Value = "1" } } } }
        });
        await svc.SaveAsync(new CostOfServicePage
        {
            Title = "V2",
            Rows = { new CostServiceRow { SortOrder = 0, Label = new LocalizedText { En = "new" } } },
            Models = { new CostServiceModel { SortOrder = 0, Name = "NewCar", Cells = { new CostServiceCell { SortOrder = 0, Value = "9" } } } }
        });

        Assert.Equal(1, await db.CostOfServicePages.CountAsync());
        var p = await svc.GetAsync();
        Assert.Equal("V2", p.Title.En);
        Assert.Single(p.Rows);
        Assert.Equal("new", p.Rows[0].Label.En);
        Assert.Single(p.Models);
        Assert.Equal("NewCar", p.Models[0].Name);
        Assert.Equal("9", p.Models[0].Cells[0].Value);
        Assert.Equal(1, await db.CostServiceModels.CountAsync());   // old model not orphaned
    }

    [Fact]
    public async Task Save_Normalizes_DropsBlanks_PadsAndTruncatesCells()
    {
        var db = NewDb(nameof(Save_Normalizes_DropsBlanks_PadsAndTruncatesCells));
        var svc = new AdminCostOfServiceService(db);

        await svc.SaveAsync(new CostOfServicePage
        {
            Title = "T",
            Rows =
            {
                new CostServiceRow { SortOrder = 0, Label = new LocalizedText { En = "r0" } },
                new CostServiceRow { SortOrder = 1, Label = new LocalizedText { En = "", Ar = "" } }, // blank → dropped
                new CostServiceRow { SortOrder = 2, Label = new LocalizedText { En = "r2" } },
            },
            Models =
            {
                new CostServiceModel { SortOrder = 0, Name = "A", Cells = { new CostServiceCell { SortOrder = 0, Value = "a0" } } }, // 1 cell → padded to 2
                new CostServiceModel { SortOrder = 1, Name = "", Cells = { new CostServiceCell { Value = "" } } },                   // blank → dropped
                new CostServiceModel { SortOrder = 2, Name = "B", Cells =
                {
                    new CostServiceCell { SortOrder = 0, Value = "b0" },
                    new CostServiceCell { SortOrder = 1, Value = "b1" },
                    new CostServiceCell { SortOrder = 2, Value = "b2" }, // 3 cells → truncated to 2
                } },
            }
        });

        var p = await svc.GetAsync();
        Assert.Equal(new[] { "r0", "r2" }, p.Rows.Select(r => r.Label.En).ToArray());     // blank row dropped + reindexed
        Assert.Equal(new[] { "A", "B" }, p.Models.Select(m => m.Name).ToArray());          // blank model dropped
        Assert.All(p.Models, m => Assert.Equal(2, m.Cells.Count));                          // every model aligned to row count
        Assert.Equal("a0", p.Models[0].Cells[0].Value);
        Assert.Null(p.Models[0].Cells[1].Value);                                            // padded blank
        // Cells must drop at the SAME index as the dropped row (idx 1), NOT tail-truncate:
        // kept rows are original idx 0 (r0) and 2 (r2), so B keeps b0 and b2 (b1 is dropped).
        Assert.Equal(new[] { "b0", "b2" }, p.Models[1].Cells.Select(c => c.Value).ToArray());
    }

    [Fact]
    public async Task Save_BlankingMiddleRowLabel_KeepsBottomRowPrice()
    {
        // Regression: blanking a non-last interval's label (without removing it) must not
        // shift prices up or silently lose the last interval's price.
        var db = NewDb(nameof(Save_BlankingMiddleRowLabel_KeepsBottomRowPrice));
        var svc = new AdminCostOfServiceService(db);

        await svc.SaveAsync(new CostOfServicePage
        {
            Rows =
            {
                new CostServiceRow { SortOrder = 0, Label = new LocalizedText { En = "5,000 KM" } },
                new CostServiceRow { SortOrder = 1, Label = new LocalizedText { En = "" } },          // middle, blank → dropped
                new CostServiceRow { SortOrder = 2, Label = new LocalizedText { En = "20,000 KM" } },
            },
            Models =
            {
                new CostServiceModel { SortOrder = 0, Name = "M8", Cells =
                {
                    new CostServiceCell { SortOrder = 0, Value = "P0" },
                    new CostServiceCell { SortOrder = 1, Value = "P1" },
                    new CostServiceCell { SortOrder = 2, Value = "P2" },
                } },
            }
        });

        var p = await svc.GetAsync();
        Assert.Equal(new[] { "5,000 KM", "20,000 KM" }, p.Rows.Select(r => r.Label.En).ToArray());
        // 20,000 KM (kept row, original idx 2) must still show its own price P2 — not P1.
        Assert.Equal(new[] { "P0", "P2" }, p.Models[0].Cells.Select(c => c.Value).ToArray());
    }
}
