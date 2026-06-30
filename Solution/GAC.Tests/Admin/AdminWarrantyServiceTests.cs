using GAC.Core.Content;
using GAC.Infrastructure.Data;
using GAC.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAC.Tests.Admin;

public class AdminWarrantyServiceTests
{
    private static ApplicationDbContext NewDb(string n) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(n).Options);

    [Fact]
    public async Task Save_Upserts_AndReplacesCallouts_DroppingBlanks()
    {
        var db = NewDb(nameof(Save_Upserts_AndReplacesCallouts_DroppingBlanks));
        var svc = new AdminWarrantyService(db);

        await svc.SaveAsync(new WarrantyPage { BannerImagePath = "/a.jpg", TermsImagePath = "/t.jpg",
            Heading = "H1", Callouts = { new WarrantyCallout { Lead = "x", Text = "y", SortOrder = 0 } } });
        await svc.SaveAsync(new WarrantyPage { BannerImagePath = "/b.jpg", TermsImagePath = "/t2.jpg",
            Heading = "H2", Callouts = {
                new WarrantyCallout { Lead = "p", Text = "q", SortOrder = 0 },
                new WarrantyCallout { Lead = "", Text = "", SortOrder = 1 },       // blank → dropped
                new WarrantyCallout { Lead = "r", Text = "s", SortOrder = 2 } } });

        Assert.Equal(1, await db.WarrantyPages.CountAsync());      // upsert, not insert-twice
        var w = await db.WarrantyPages.Include(p => p.Callouts).FirstAsync();
        Assert.Equal("H2", w.Heading.En);
        Assert.Equal("/b.jpg", w.BannerImagePath);
        Assert.Equal(2, w.Callouts.Count);                        // replaced, blank dropped
        Assert.DoesNotContain(w.Callouts, c => c.Lead.En == "x");
    }
}
