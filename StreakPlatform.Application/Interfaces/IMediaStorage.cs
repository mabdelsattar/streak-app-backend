using StreakPlatform.Application.DTOs;

namespace StreakPlatform.Application.Interfaces;

public record StoredMedia(string Url, string ContentType, long SizeBytes);

public interface IMediaStorage
{
    /// <summary>Server-side multipart upload (used by /api/media/upload). Kept for local dev simplicity.</summary>
    Task<StoredMedia> SaveAsync(
        Guid userId,
        Stream content,
        string contentType,
        long sizeBytes,
        string originalFileName,
        CancellationToken ct = default);

    /// <summary>
    /// Issues an upload ticket. The frontend uploads directly to <c>UploadUrl</c> using <c>UploadMethod</c>.
    /// For GCS this is a signed PUT URL; for Local it's a multipart POST back to the API.
    /// </summary>
    Task<UploadTicketDto> CreateUploadTicketAsync(
        Guid userId,
        string kind,                  // "image" | "audio" | "video"
        string contentType,
        long sizeBytes,
        CancellationToken ct = default);
}
