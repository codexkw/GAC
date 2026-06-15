using GAC.Core.Content;

namespace GAC.Core.Services;

public interface IMediaService
{
    Task<MediaUploadResult> UploadAsync(Stream content, string originalFileName, string contentType, long length, CancellationToken ct = default);
    Task<IReadOnlyList<MediaAsset>> ListAsync(CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}
