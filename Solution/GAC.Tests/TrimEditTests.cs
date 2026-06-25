using GAC.Core.Content;
using GAC.Infrastructure.Data;
using GAC.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAC.Tests;

public class TrimEditTests
{
    private static ApplicationDbContext NewDb(string n) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(n).Options);

    private sealed class NoopSanitizer : GAC.Core.Services.IHtmlSanitizerService
    {
        public string Sanitize(string? html) => html ?? "";
    }

    [Fact]
    public async Task UpdateTrimAsync_Persists_Editable_Fields_And_Preserves_Order_And_PriceRows()
    {
        var db = NewDb(nameof(UpdateTrimAsync_Persists_Editable_Fields_And_Preserves_Order_And_PriceRows));
        db.Vehicles.Add(new Vehicle { Id = 1, Slug = "emkoo", Name = new LocalizedText { En = "EMKOO" } });
        db.Set<Trim>().Add(new Trim
        {
            Id = 5,
            VehicleId = 1,
            SortOrder = 2,
            Name = new LocalizedText { En = "GL" },
            ModelLabel = new LocalizedText { En = "2026" },
            SpecPdf = "/pdfs/emkoo-specifications.pdf",
            ImagePath = "/old.png",
            PriceRows = { new TrimPriceRow { Id = 9, Text = new LocalizedText { En = "Total: 9,000 KWD" } } }
        });
        await db.SaveChangesAsync();

        var svc = new AdminVehicleService(db, new NoopSanitizer());
        // Fresh, untracked instance — mirrors MVC model binding handing the service a new object.
        var update = new Trim
        {
            Id = 5,
            Name = new LocalizedText { En = "GLX" },
            ModelLabel = new LocalizedText { En = "2027" },
            SpecPdf = "/uploads/new-emkoo-spec.pdf",
            ImagePath = "/new.png"
        };

        Assert.True(await svc.UpdateTrimAsync(update));

        var saved = await db.Set<Trim>().Include(t => t.PriceRows).FirstAsync(t => t.Id == 5);
        Assert.Equal("/uploads/new-emkoo-spec.pdf", saved.SpecPdf);
        Assert.Equal("GLX", saved.Name.En);
        Assert.Equal("2027", saved.ModelLabel.En);
        Assert.Equal("/new.png", saved.ImagePath);
        // Untouched relationships/order must survive a field edit.
        Assert.Equal(2, saved.SortOrder);
        Assert.Equal(1, saved.VehicleId);
        Assert.Single(saved.PriceRows);
    }

    [Fact]
    public async Task UpdateTrimAsync_Returns_False_For_Missing_Trim()
    {
        var db = NewDb(nameof(UpdateTrimAsync_Returns_False_For_Missing_Trim));
        var svc = new AdminVehicleService(db, new NoopSanitizer());
        Assert.False(await svc.UpdateTrimAsync(new Trim { Id = 404, SpecPdf = "/uploads/x.pdf" }));
    }
}
