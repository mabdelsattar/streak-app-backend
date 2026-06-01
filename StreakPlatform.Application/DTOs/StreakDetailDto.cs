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
    int MissedDays,
    int RecoveryUnitCost,
    int RecoveryCost,
    bool CanAfford,
    string CheckInType,
    string? CheckInButtonLabel,
    bool IsPublic,
    IReadOnlyList<ParticipantStatusDto> Participants);
