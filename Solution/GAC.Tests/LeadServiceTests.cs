using GAC.Core.Content;
using GAC.Infrastructure.Data;
using GAC.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GAC.Tests;

public class LeadServiceTests
{
    private static ApplicationDbContext NewDb(string name)
    {
        var sp = new ServiceCollection()
            .AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase(name))
            .BuildServiceProvider();
        return sp.GetRequiredService<ApplicationDbContext>();
    }

    [Fact]
    public async Task CreateAsync_PersistsLead()
    {
        var db = NewDb("lead-create");
        var svc = new LeadService(db);

        await svc.CreateAsync(new Lead
        {
            FormType = FormType.TestDrive,
            Name = "Mr Ada Lovelace",
            Email = "ada@example.com",
            Phone = "12345678"
        });

        var lead = await db.Leads.SingleAsync();
        Assert.Equal("Mr Ada Lovelace", lead.Name);
        Assert.Equal(FormType.TestDrive, lead.FormType);
        Assert.Equal(LeadStatus.New, lead.Status);
    }
}
