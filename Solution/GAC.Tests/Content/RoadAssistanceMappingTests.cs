using GAC.Core.Content;
using GAC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAC.Tests.Content;

public class RoadAssistanceMappingTests
{
    private static ApplicationDbContext NewDb(string n) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(n).Options);

    [Fact]
    public async Task RoadAssistancePage_RoundTrips()
    {
        var db = NewDb(nameof(RoadAssistancePage_RoundTrips));
        db.RoadAssistancePages.Add(new RoadAssistancePage
        {
            Heading = new LocalizedText { En = "Roadside Assistance", Ar = "المساعدة على الطريق" },
            Intro = new LocalizedText { En = "line1\nline2", Ar = "سطر" },
            ContactLead = new LocalizedText { En = "Getting In Touch", Ar = "للتواصل" },
            ContactText = new LocalizedText { En = "Call us", Ar = "اتصل بنا" },
            PhoneNumber = "1833334",
            CallButtonLabel = new LocalizedText { En = "Call 1833334", Ar = "اتصل 1833334" }
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var r = await db.RoadAssistancePages.AsNoTracking().FirstAsync();
        Assert.Equal("Roadside Assistance", r.Heading.En);
        Assert.Equal("المساعدة على الطريق", r.Heading.Ar);
        Assert.Equal("1833334", r.PhoneNumber);
        Assert.Equal("Call 1833334", r.CallButtonLabel.En);
        Assert.Equal("Getting In Touch", r.ContactLead.En);
    }
}
