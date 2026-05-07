namespace StreakPlatform.Application.DTOs;

public record UploadTicketDto(
    string UploadUrl,
    string UploadMethod,        // "POST_MULTIPART" | "PUT_BINARY"
    string? UploadFieldName,    // "file" for POST_MULTIPART, null for PUT_BINARY
    string? PublicUrl,          // Known up-front for PUT_BINARY (GCS); null for multipart (server returns it)
    string ContentType,
    long MaxSizeBytes,
    DateTime ExpiresAt);
