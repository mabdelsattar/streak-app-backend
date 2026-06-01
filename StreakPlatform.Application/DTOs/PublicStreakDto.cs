namespace StreakPlatform.Application.DTOs;

/// <summary>
/// Public-discovery view of a streak. Deliberately excludes the invite code so the
/// public listing doesn't double as a way to bypass the join cost on private streaks.
/// </summary>
public record PublicStreakDto(
    Guid Id,
    string Name,
    string? Description,
    int ParticipantCount,
    string CheckInType,
    DateTime CreatedAt,
    string CreatorDisplayName);

public record JoinPublicResultDto(
    Guid StreakId,
    int PointsCharged,
    int NewBalance);
