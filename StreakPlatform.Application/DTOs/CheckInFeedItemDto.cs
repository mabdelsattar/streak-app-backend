namespace StreakPlatform.Application.DTOs;

public record CheckInFeedItemDto(
    Guid Id,
    Guid UserId,
    string DisplayName,
    DateOnly Date,
    DateTime CreatedAt,
    string? Note,
    string? MediaUrl,
    string? MediaContentType,
    int? MediaDurationSeconds,
    int LikeCount,
    int DislikeCount,
    string? MyReaction,   // "Like" | "Dislike" | null
    bool IsMine);
