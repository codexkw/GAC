using System.Text;
using GAC.Core.Services;
using GAC.Infrastructure.Data;
using GAC.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace GAC.Tests.Admin;

public class MediaServiceTests : IDisposable
{
    private readonly string _root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "gacmedia-" + System.Guid.NewGuid().ToString("N"));

    private ApplicationDbContext NewDb(string n) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(n).Options);

    private MediaService NewSvc(ApplicationDbContext db) =>
        new(db, Options.Create(new MediaOptions { Root = _root, PublicPrefix = "/uploads", MaxBytes = 1024 }));

    [Fact]
    public async Task Upload_Image_WritesFile_AndTracksAsset()
    {
        var db = NewDb(nameof(Upload_Image_WritesFile_AndTracksAsset));
        var svc = NewSvc(db);
        var bytes = Encoding.UTF8.GetBytes("fake-image");
        var res = await svc.UploadAsync(new MemoryStream(bytes), "Photo Name.png", "image/png", bytes.Length);

        Assert.True(res.Ok);
        Assert.NotNull(res.Path);
        Assert.StartsWith("/uploads/", res.Path);
        Assert.True(System.IO.File.Exists(System.IO.Path.Combine(_root, System.IO.Path.GetFileName(res.Path!))));
        Assert.Equal(1, await db.MediaAssets.CountAsync());
    }

    [Fact]
    public async Task Upload_RejectsNonImage()
    {
        var db = NewDb(nameof(Upload_RejectsNonImage));
        var res = await NewSvc(db).UploadAsync(new MemoryStream([1, 2, 3]), "x.exe", "application/octet-stream", 3);
        Assert.False(res.Ok);
        Assert.Equal(0, await db.MediaAssets.CountAsync());
    }

    [Fact]
    public async Task Upload_RejectsOversize()
    {
        var db = NewDb(nameof(Upload_RejectsOversize));
        var res = await NewSvc(db).UploadAsync(new MemoryStream(new byte[2048]), "big.png", "image/png", 2048);
        Assert.False(res.Ok);
    }

    public void Dispose()
    {
        if (System.IO.Directory.Exists(_root)) System.IO.Directory.Delete(_root, true);
    }
}
