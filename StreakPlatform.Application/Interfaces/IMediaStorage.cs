namespace StreakPlatform.Application.Interfaces;

public record StoredMedia(string Url, string ContentType, long SizeBytes);

public interface IMediaStorage
{
    /// <summary>
    /// Persists the stream and returns a publicly-resolvable URL (relative to API host).
    /// Implementations are responsible for validating size and content-type, sanitizing extension,
    /// and isolating files per user.
    /// </summary>
    Task<StoredMedia> SaveAsync(
        Guid userId,
        Stream content,
        string contentType,
        long sizeBytes,
        string originalFileName,
        CancellationToken ct = default);
}
