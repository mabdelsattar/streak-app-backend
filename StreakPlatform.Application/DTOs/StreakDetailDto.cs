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
    int MyPointsBalance,
    ProtectionDto? MyProtection,
    bool CanRestoreToday,
    int RestoreCost,
    string CheckInType,
    string? CheckInButtonLabel,
    IReadOnlyList<ParticipantStatusDto> Participants);
