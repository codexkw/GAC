using GAC.Core.Services;
using GAC.Infrastructure.Data;
using GAC.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace GAC.Tests;

public class MediaServiceTests : IDisposable
{
    private readonly List<string> _tempRoots = new();

    private static ApplicationDbContext NewDb(string n) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(n).Options);

    private MediaService NewSvc(ApplicationDbContext db)
    {
        var root = Path.Combine(Path.GetTempPath(), "gactest-" + Guid.NewGuid().ToString("N"));
        _tempRoots.Add(root);
        var opt = Options.Create(new MediaOptions { Root = root, PublicPrefix = "/uploads" });
        return new MediaService(db, opt);
    }

    [Fact]
    public async Task Accepts_Pdf_Upload()
    {
        var db = NewDb(nameof(Accepts_Pdf_Upload));
        var svc = NewSvc(db);
        using var ms = new MemoryStream(new byte[] { 1, 2, 3 });
        var res = await svc.UploadAsync(ms, "spec.pdf", "application/pdf", 3);
        Assert.True(res.Ok);
        Assert.NotNull(res.Path);
        Assert.EndsWith(".pdf", res.Path);
    }

    [Fact]
    public async Task Still_Accepts_Png()
    {
        var db = NewDb(nameof(Still_Accepts_Png));
        var svc = NewSvc(db);
        using var ms = new MemoryStream(new byte[] { 1, 2, 3 });
        var res = await svc.UploadAsync(ms, "pic.png", "image/png", 3);
        Assert.True(res.Ok);
    }

    [Fact]
    public async Task Rejects_Disallowed_Type()
    {
        var db = NewDb(nameof(Rejects_Disallowed_Type));
        var svc = NewSvc(db);
        using var ms = new MemoryStream(new byte[] { 1, 2, 3 });
        var res = await svc.UploadAsync(ms, "evil.exe", "application/octet-stream", 3);
        Assert.False(res.Ok);
    }

    [Fact]
    public async Task Rejects_Pdf_Over_PdfMaxBytes()
    {
        var db = NewDb(nameof(Rejects_Pdf_Over_PdfMaxBytes));
        var root = Path.Combine(Path.GetTempPath(), "gactest-" + Guid.NewGuid().ToString("N"));
        _tempRoots.Add(root);
        var opt = Options.Create(new MediaOptions { Root = root, PublicPrefix = "/uploads", PdfMaxBytes = 10 });
        var svc = new MediaService(db, opt);
        using var ms = new MemoryStream(new byte[11]);
        var res = await svc.UploadAsync(ms, "big.pdf", "application/pdf", 11);
        Assert.False(res.Ok);
    }

    public void Dispose()
    {
        foreach (var root in _tempRoots)
            if (Directory.Exists(root)) Directory.Delete(root, true);
    }
}
