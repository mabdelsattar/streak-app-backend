namespace StreakPlatform.Application.DTOs;

public record PayDebtResultDto(
    Guid StreakId,
    int DaysCovered,
    int PointsCharged,
    int NewBalance,
    IReadOnlyList<DateOnly> CoveredDates);
