using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Options;
using StreakPlatform.Application.Common;
using StreakPlatform.Application.DTOs;
using StreakPlatform.Application.Interfaces;

namespace StreakPlatform.Infrastructure.Storage;

/// <summary>
/// Stores media in Google Cloud Storage. The frontend uploads directly via short-lived
/// V4 signed PUT URLs, so binary never traverses the .NET API.
/// Bucket must be public-read (or have appropriate IAM) for the returned PublicUrl to be servable.
/// </summary>
public class GcsMediaStorage : IMediaStorage
{
    private readonly AppOptions _options;
    private readonly StorageClient _storage;
    private readonly UrlSigner _signer;

    public GcsMediaStorage(IOptions<AppOptions> options)
    {
        _options = options.Value;

        if (string.IsNullOrWhiteSpace(_options.Gcs.BucketName))
            throw new InvalidOperationException("Gcs:BucketName is required when MediaStorage:Provider=Gcs.");

        var credential = string.IsNullOrWhiteSpace(_options.Gcs.CredentialsPath)
            ? GoogleCredential.GetApplicationDefault()
            : GoogleCredential.FromFile(_options.Gcs.CredentialsPath);

        _storage = StorageClient.Create(credential);
        _signer = UrlSigner.FromCredential(credential);
    }

    /// <summary>
    /// Server-side upload (used for the legacy /api/media/upload multipart endpoint).
    /// Streams the file directly into the bucket.
    /// </summary>
    public async Task<StoredMedia> SaveAsync(
        Guid userId, Stream content, string contentType, long sizeBytes,
        string originalFileName, CancellationToken ct = default)
    {
        ValidateUpload(contentType, sizeBytes);
        var (objectName, _) = NewObjectKey(userId, KindFromContentType(contentType), contentType);
        await _storage.UploadObjectAsync(_options.Gcs.BucketName, objectName, contentType, content, cancellationToken: ct);
        return new StoredMedia(PublicUrl(objectName), contentType, sizeBytes);
    }

    public Task<UploadTicketDto> CreateUploadTicketAsync(
        Guid userId, string kind, string contentType, long sizeBytes, CancellationToken ct = default)
    {
        ValidateUpload(contentType, sizeBytes);
        ValidateKindAgainstContentType(kind, contentType);
        var maxBytes = MaxSizeForKind(kind);
        if (sizeBytes > maxBytes)
            throw new ArgumentException($"File exceeds {maxBytes / (1024 * 1024)} MB limit for {kind}.", nameof(sizeBytes));

        var (objectName, _) = NewObjectKey(userId, kind, contentType);
        var ttl = TimeSpan.FromMinutes(_options.Gcs.UploadUrlTtlMinutes);

        var template = UrlSigner.RequestTemplate
            .FromBucket(_options.Gcs.BucketName)
            .WithObjectName(objectName)
            .WithHttpMethod(HttpMethod.Put)
            .WithContentHeaders(new Dictionary<string, IEnumerable<string>>
            {
                ["Content-Type"] = new[] { contentType }
            });
        var sigOptions = UrlSigner.Options.FromDuration(ttl).WithSigningVersion(SigningVersion.V4);
        var signedUrl = _signer.Sign(template, sigOptions);

        return Task.FromResult(new UploadTicketDto(
            UploadUrl: signedUrl,
            UploadMethod: "PUT_BINARY",
            UploadFieldName: null,
            PublicUrl: PublicUrl(objectName),
            ContentType: contentType,
            MaxSizeBytes: maxBytes,
            ExpiresAt: DateTime.UtcNow.Add(ttl)));
    }

    private (string ObjectName, string Extension) NewObjectKey(Guid userId, string kind, string contentType)
    {
        var ext = ExtensionFor(contentType);
        var name = $"{userId}/{kind}/{Guid.NewGuid():N}{ext}";
        return (name, ext);
    }

    private string PublicUrl(string objectName) =>
        $"{_options.Gcs.PublicBaseUrl.TrimEnd('/')}/{_options.Gcs.BucketName}/{objectName}";

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

    private static string KindFromContentType(string contentType) =>
        contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ? "image" :
        contentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase) ? "audio" :
        contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase) ? "video" :
        "other";

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
