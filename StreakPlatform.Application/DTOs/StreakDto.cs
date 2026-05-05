namespace StreakPlatform.Application.DTOs;

public record StreakDto(
    Guid Id,
    string Name,
    int CurrentCount,
    DateOnly? LastCheckInDate,
    bool CanCheckInToday);
