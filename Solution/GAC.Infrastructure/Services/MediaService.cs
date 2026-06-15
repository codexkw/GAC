using GAC.Core.Content;
using GAC.Core.Services;
using GAC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GAC.Infrastructure.Services;

public class MediaService : IMediaService
{
    private static readonly HashSet<string> AllowedExt =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp", ".gif", ".svg" };
    private static readonly HashSet<string> AllowedCt =
        new(StringComparer.OrdinalIgnoreCase) { "image/jpeg", "image/png", "image/webp", "image/gif", "image/svg+xml" };

    private readonly ApplicationDbContext _db;
    private readonly MediaOptions _opt;

    public MediaService(ApplicationDbContext db, IOptions<MediaOptions> opt)
    {
        _db = db;
        _opt = opt.Value;
    }

    public async Task<MediaUploadResult> UploadAsync(Stream content, string originalFileName, string contentType, long length, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(originalFileName);
        if (string.IsNullOrEmpty(ext) || !AllowedExt.Contains(ext) || !AllowedCt.Contains(contentType))
            return new MediaUploadResult(false, null, "Only image files (jpg, png, webp, gif, svg) are allowed.");
        if (length <= 0 || length > _opt.MaxBytes)
            return new MediaUploadResult(false, null, $"File must be between 1 byte and {_opt.MaxBytes / 1024} KB.");

        Directory.CreateDirectory(_opt.Root);
        var safeName = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        var fullPath = Path.Combine(_opt.Root, safeName);
        await using (var fs = File.Create(fullPath))
            await content.CopyToAsync(fs, ct);

        var publicPath = $"{_opt.PublicPrefix.TrimEnd('/')}/{safeName}";
        var asset = new MediaAsset
        {
            Path = publicPath,
            OriginalFileName = originalFileName,
            UploadedAt = DateTimeOffset.UtcNow
        };
        _db.MediaAssets.Add(asset);
        await _db.SaveChangesAsync(ct);
        return new MediaUploadResult(true, publicPath, null);
    }

    public async Task<IReadOnlyList<MediaAsset>> ListAsync(CancellationToken ct = default)
        => await _db.MediaAssets.AsNoTracking().OrderByDescending(m => m.UploadedAt).ToListAsync(ct);

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var asset = await _db.MediaAssets.FindAsync([id], ct);
        if (asset is null) return false;
        var full = Path.Combine(_opt.Root, Path.GetFileName(asset.Path));
        if (File.Exists(full)) File.Delete(full);
        _db.MediaAssets.Remove(asset);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
