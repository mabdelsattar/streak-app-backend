namespace StreakPlatform.Application.DTOs;

public record StreakSummaryDto(
    Guid Id,
    string Name,
    string? Description,
    int CurrentCount,
    bool CheckedInToday,
    int ParticipantCount,
    string CheckInType,
    string? CheckInButtonLabel,
    bool IsPublic);
