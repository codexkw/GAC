using GAC.Core.Content;
using GAC.Core.Services;
using GAC.Infrastructure.Data;
using GAC.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAC.Tests.Admin;

public class AdminLeadServiceTests
{
    private static ApplicationDbContext NewDb(string name) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(name).Options);

    private static async Task Seed(ApplicationDbContext db)
    {
        db.Leads.AddRange(
            new Lead { FormType = FormType.TestDrive, Status = LeadStatus.New, Name = "A", CreatedAt = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero) },
            new Lead { FormType = FormType.Quote, Status = LeadStatus.Contacted, Name = "B", CreatedAt = new DateTimeOffset(2026, 6, 10, 0, 0, 0, TimeSpan.Zero) });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task List_FiltersByStatus()
    {
        var db = NewDb(nameof(List_FiltersByStatus)); await Seed(db);
        var svc = new AdminLeadService(db);
        var rows = await svc.ListAsync(new LeadFilter(null, LeadStatus.New, null, null));
        Assert.Single(rows);
        Assert.Equal("A", rows[0].Name);
    }

    [Fact]
    public async Task SetStatus_Updates()
    {
        var name = nameof(SetStatus_Updates);
        var db = NewDb(name); await Seed(db);
        var svc = new AdminLeadService(db);
        var id = (await db.Leads.FirstAsync()).Id;
        Assert.True(await svc.SetStatusAsync(id, LeadStatus.Closed));

        using var verify = NewDb(name);
        Assert.Equal(LeadStatus.Closed, (await verify.Leads.FindAsync(id))!.Status);
    }

    [Fact]
    public async Task List_FiltersByFormType()
    {
        var db = NewDb(nameof(List_FiltersByFormType)); await Seed(db);
        var svc = new AdminLeadService(db);
        var rows = await svc.ListAsync(new LeadFilter(FormType.Quote, null, null, null));
        Assert.Single(rows);
        Assert.Equal("B", rows[0].Name);
    }

    [Fact]
    public async Task List_FiltersByDateRange_InclusiveBoundaries()
    {
        var db = NewDb(nameof(List_FiltersByDateRange_InclusiveBoundaries)); await Seed(db);
        var svc = new AdminLeadService(db);

        // From only: 2026-06-01 lead is before From and excluded; 2026-06-10 lead included (From inclusive of its day).
        var fromOnly = await svc.ListAsync(new LeadFilter(null, null, new DateOnly(2026, 6, 10), null));
        Assert.Single(fromOnly);
        Assert.Equal("B", fromOnly[0].Name);

        // To only: lead created 2026-06-01T00:00Z is included when To = 2026-06-01 (To covers the whole day via < To.AddDays(1)).
        var toOnly = await svc.ListAsync(new LeadFilter(null, null, null, new DateOnly(2026, 6, 1)));
        Assert.Single(toOnly);
        Assert.Equal("A", toOnly[0].Name);

        // Window between the two leads returns nothing.
        var between = await svc.ListAsync(new LeadFilter(null, null, new DateOnly(2026, 6, 2), new DateOnly(2026, 6, 9)));
        Assert.Empty(between);
    }

    [Fact]
    public async Task Get_ReturnsLead_OrNull()
    {
        var db = NewDb(nameof(Get_ReturnsLead_OrNull)); await Seed(db);
        var svc = new AdminLeadService(db);
        var seeded = await db.Leads.FirstAsync();

        var found = await svc.GetAsync(seeded.Id);
        Assert.NotNull(found);
        Assert.Equal(seeded.Name, found!.Name);

        Assert.Null(await svc.GetAsync(999999));
    }

    [Fact]
    public async Task Delete_Removes()
    {
        var db = NewDb(nameof(Delete_Removes)); await Seed(db);
        var svc = new AdminLeadService(db);
        var id = (await db.Leads.FirstAsync()).Id;
        Assert.True(await svc.DeleteAsync(id));
        Assert.Null(await db.Leads.FindAsync(id));
    }
}
