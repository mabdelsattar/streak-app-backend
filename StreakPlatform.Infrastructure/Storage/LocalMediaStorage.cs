using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using StreakPlatform.Application.Common;
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
        if (sizeBytes <= 0)
            throw new ArgumentException("File is empty.", nameof(sizeBytes));
        if (sizeBytes > _options.MaxMediaSizeBytes)
            throw new ArgumentException($"File exceeds {_options.MaxMediaSizeBytes / (1024 * 1024)} MB limit.", nameof(sizeBytes));
        if (!_options.AllowedMediaContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException($"Unsupported content type: {contentType}", nameof(contentType));

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

    private static string ExtensionFor(string contentType) => contentType.ToLowerInvariant() switch
    {
        "image/jpeg" => ".jpg",
        "image/png" => ".png",
        "image/webp" => ".webp",
        _ => ".bin"
    };
}
