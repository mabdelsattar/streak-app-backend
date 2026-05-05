namespace StreakPlatform.Application.DTOs;

public record StreakDetailDto(
    Guid Id,
    string Name,
    string? Description,
    string InviteCode,
    string InviteUrl,
    DateTime CreatedAt,
    Guid CreatedBy,
    int MyCurrentCount,
    bool MyCheckedInToday,
    IReadOnlyList<ParticipantStatusDto> Participants);
