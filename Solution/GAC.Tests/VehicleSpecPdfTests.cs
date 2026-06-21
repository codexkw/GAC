using GAC.Core.Content;
using GAC.Infrastructure.Data;
using GAC.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAC.Tests;

public class VehicleSpecPdfTests
{
    private static ApplicationDbContext NewDb(string n) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(n).Options);

    private sealed class NoopSanitizer : GAC.Core.Services.IHtmlSanitizerService
    {
        public string Sanitize(string? html) => html ?? "";
    }

    [Fact]
    public async Task UpdateAsync_Persists_SpecPdf()
    {
        var db = NewDb(nameof(UpdateAsync_Persists_SpecPdf));
        db.Vehicles.Add(new Vehicle { Id = 1, Slug = "gs4", Name = new LocalizedText { En = "GS4 MAX" } });
        await db.SaveChangesAsync();

        var svc = new AdminVehicleService(db, new NoopSanitizer());
        // Distinct, untracked instance — mirrors MVC model binding handing the service a fresh object.
        var update = new Vehicle { Id = 1, Slug = "gs4", Name = new LocalizedText { En = "GS4 MAX" }, SpecPdf = "/uploads/gs4-spec.pdf" };
        Assert.True(await svc.UpdateAsync(update));

        Assert.Equal("/uploads/gs4-spec.pdf", (await db.Vehicles.FindAsync(1))!.SpecPdf);
    }
}
