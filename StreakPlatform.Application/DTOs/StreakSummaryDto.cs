namespace StreakPlatform.Application.DTOs;

public record StreakSummaryDto(
    Guid Id,
    string Name,
    string? Description,
    int CurrentCount,
    bool CheckedInToday,
    int ParticipantCount,
    bool HasActiveProtection,
    string CheckInType,
    string? CheckInButtonLabel);
