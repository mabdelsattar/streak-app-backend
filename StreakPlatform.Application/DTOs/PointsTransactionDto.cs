namespace StreakPlatform.Application.DTOs;

public record PointsTransactionDto(
    Guid Id,
    int Delta,
    string Reason,
    Guid? RelatedStreakId,
    DateTime CreatedAt);
