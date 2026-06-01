using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using StreakPlatform.Application.Common;
using StreakPlatform.Application.DTOs;
using StreakPlatform.Application.Interfaces;

namespace StreakPlatform.Infrastructure.Storage;

/// <summary>
/// Saves uploaded media to a local directory under the API content root.
/// Files are organized as {MediaStorageDirectory}/{userId}/{guid}.{ext}
/// and exposed publicly at {MediaPublicPath}/{userId}/{guid}.{ext} via static files.
/// </summary>
public class LocalMediaStorage : IMediaStorage
{
    private readonly AppOptions _options;
    private readonly IHostEnvironment _env;

    public LocalMediaStorage(IOptions<AppOptions> options, IHostEnvironment env)
    {
        _options = options.Value;
        _env = env;
    }

    public async Task<StoredMedia> SaveAsync(
        Guid userId, Stream content, string contentType, long sizeBytes,
        string originalFileName, CancellationToken ct = default)
    {
        ValidateUpload(contentType, sizeBytes);

        var ext = ExtensionFor(contentType);
        var rootPath = Path.Combine(_env.ContentRootPath, _options.MediaStorageDirectory, userId.ToString());
        Directory.CreateDirectory(rootPath);

        var filename = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(rootPath, filename);

        await using (var fs = File.Create(fullPath))
        {
            await content.CopyToAsync(fs, ct);
        }

        var url = $"{_options.MediaPublicPath.TrimEnd('/')}/{userId}/{filename}";
        return new StoredMedia(url, contentType, sizeBytes);
    }

    /// <summary>
    /// Local provider doesn't issue signed URLs. Instead returns a ticket pointing back at the
    /// existing multipart upload endpoint, so the frontend uses identical code regardless of provider.
    /// </summary>
    public Task<UploadTicketDto> CreateUploadTicketAsync(
        Guid userId, string kind, string contentType, long sizeBytes, CancellationToken ct = default)
    {
        ValidateUpload(contentType, sizeBytes);
        ValidateKindAgainstContentType(kind, contentType);

        var maxBytes = MaxSizeForKind(kind);
        // Return a path-only URL. The frontend prepends its own apiBaseUrl so the
        // upload always targets the API (not the static web app host).
        return Task.FromResult(new UploadTicketDto(
            UploadUrl: "/api/media/upload",
            UploadMethod: "POST_MULTIPART",
            UploadFieldName: "file",
            PublicUrl: null,                        // server returns the URL after upload
            ContentType: contentType,
            MaxSizeBytes: maxBytes,
            ExpiresAt: DateTime.UtcNow.AddMinutes(10)));
    }

    private void ValidateUpload(string contentType, long sizeBytes)
    {
        if (sizeBytes <= 0)
            throw new ArgumentException("File is empty.", nameof(sizeBytes));
        if (!_options.AllowedMediaContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException($"Unsupported content type: {contentType}", nameof(contentType));
    }

    private void ValidateKindAgainstContentType(string kind, string contentType)
    {
        var lc = contentType.ToLowerInvariant();
        var ok = kind switch
        {
            "image" => lc.StartsWith("image/"),
            "audio" => lc.StartsWith("audio/"),
            "video" => lc.StartsWith("video/"),
            _ => false
        };
        if (!ok) throw new ArgumentException($"Content type {contentType} doesn't match kind {kind}.");
    }

    private long MaxSizeForKind(string kind) => kind switch
    {
        "image" => _options.MaxMediaSizeBytes,
        "audio" => _options.MaxAudioSizeBytes,
        "video" => _options.MaxVideoSizeBytes,
        _ => _options.MaxMediaSizeBytes
    };

    private static string ExtensionFor(string contentType) => contentType.ToLowerInvariant() switch
    {
        "image/jpeg" => ".jpg",
        "image/png" => ".png",
        "image/webp" => ".webp",
        "audio/webm" => ".webm",
        "audio/ogg" => ".ogg",
        "audio/mp4" => ".m4a",
        "video/webm" => ".webm",
        "video/mp4" => ".mp4",
        _ => ".bin"
    };
}
