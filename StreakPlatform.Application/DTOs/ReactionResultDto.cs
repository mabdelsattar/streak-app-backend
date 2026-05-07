namespace StreakPlatform.Application.DTOs;

public record ReactionResultDto(
    Guid CheckInId,
    string? CurrentReaction,   // "Like" | "Dislike" | null (after removal)
    int LikeCount,
    int DislikeCount,
    int ReactorBalance,
    int AuthorBalance);
