namespace StreakPlatform.Application.DTOs;

public record RestoreResultDto(
    Guid StreakId,
    DateOnly RestoredDate,
    int NewBalance,
    int CurrentCount);
