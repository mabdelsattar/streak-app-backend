using StreakPlatform.Application.DTOs;

namespace StreakPlatform.Application.Interfaces;

public interface IStreakService
{
    Task<StreakDetailDto> CreateAsync(string firebaseUid, CreateStreakRequest req, CancellationToken ct = default);
    Task<IReadOnlyList<StreakSummaryDto>> GetMyStreaksAsync(string firebaseUid, CancellationToken ct = default);
    Task<StreakDetailDto> GetDetailAsync(string firebaseUid, Guid streakId, CancellationToken ct = default);
    Task<StreakDetailDto> JoinByInviteCodeAsync(string firebaseUid, string inviteCode, CancellationToken ct = default);
    Task<InviteDto> GetInviteAsync(string firebaseUid, Guid streakId, CancellationToken ct = default);

    /// <summary>Lists public streaks the current user is NOT already participating in.</summary>
    Task<IReadOnlyList<PublicStreakDto>> GetPublicStreaksAsync(
        string firebaseUid, int take, int skip, string? search, CancellationToken ct = default);

    /// <summary>Direct join into a public streak (no invite code needed).</summary>
    Task<StreakDetailDto> JoinPublicAsync(string firebaseUid, Guid streakId, CancellationToken ct = default);
}
