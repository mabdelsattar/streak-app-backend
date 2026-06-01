namespace StreakPlatform.Application.DTOs;

public record DebtDto(
    Guid StreakId,
    int MissedDays,
    int RecoveryUnitCost,
    int TotalCost,
    int Balance,
    bool CanAfford,
    IReadOnlyList<DateOnly> MissedDates);
