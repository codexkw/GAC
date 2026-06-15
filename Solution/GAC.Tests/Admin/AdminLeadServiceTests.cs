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
        var db = NewDb(nameof(SetStatus_Updates)); await Seed(db);
        var svc = new AdminLeadService(db);
        var id = (await db.Leads.FirstAsync()).Id;
        Assert.True(await svc.SetStatusAsync(id, LeadStatus.Closed));
        Assert.Equal(LeadStatus.Closed, (await db.Leads.FindAsync(id))!.Status);
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
