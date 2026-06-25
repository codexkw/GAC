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

    [Fact]
    public async Task UpdatePreviewLinkAsync_Sets_Trims_And_Clears()
    {
        var db = NewDb(nameof(UpdatePreviewLinkAsync_Sets_Trims_And_Clears));
        db.Vehicles.Add(new Vehicle { Id = 1, Slug = "emkoo", Name = new LocalizedText { En = "EMKOO" } });
        await db.SaveChangesAsync();
        var svc = new AdminVehicleService(db, new NoopSanitizer());

        Assert.True(await svc.UpdatePreviewLinkAsync(1, "  https://example.com/preview  "));
        Assert.Equal("https://example.com/preview", (await db.Vehicles.FindAsync(1))!.SpecPdf);

        // Blank clears the link (hides the Preview button).
        Assert.True(await svc.UpdatePreviewLinkAsync(1, "   "));
        Assert.Null((await db.Vehicles.FindAsync(1))!.SpecPdf);

        Assert.False(await svc.UpdatePreviewLinkAsync(404, "x"));
    }
}

public class VehicleSpecPdfRenderTests : IClassFixture<DevWebApplicationFactory>
{
    private readonly DevWebApplicationFactory _factory;
    public VehicleSpecPdfRenderTests(DevWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Gs4_Renders_And_OmitsFieldDrivenSpecCta_WhenNoSpecPdf()
    {
        var response = await _factory.CreateClient().GetAsync("/gs4");
        response.EnsureSuccessStatusCode(); // rewritten Detail.cshtml renders without error
        var html = await response.Content.ReadAsStringAsync();
        // gs4 has no SpecPdf set in the DB -> the field-driven Specifications CTA must not render.
        // If the @if (specBtn) conditional in Detail.cshtml were broken to always render the section,
        // "mp-spec-cta" would appear here and this test would fail — making this a genuine code check.
        // (Removing the legacy in-body /pdfs/ anchor from live data is handled by the
        //  reviewed content SQL section 3b/3c at deploy + manual QA, not asserted here.)
        Assert.DoesNotContain("mp-spec-cta", html);
    }
}
