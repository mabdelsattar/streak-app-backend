namespace StreakPlatform.Application.DTOs;

public record CheckInResultDto(
    Guid StreakId,
    DateOnly Date,
    int CurrentCount,
    int PointsAwarded,
    int NewBalance,
    bool ProtectionConsumed);
