using StreakPlatform.Application.DTOs;

namespace StreakPlatform.Application.Interfaces;

public interface IStreakService
{
    Task<StreakDetailDto> CreateAsync(string firebaseUid, CreateStreakRequest req, CancellationToken ct = default);
    Task<IReadOnlyList<StreakSummaryDto>> GetMyStreaksAsync(string firebaseUid, CancellationToken ct = default);
    Task<StreakDetailDto> GetDetailAsync(string firebaseUid, Guid streakId, CancellationToken ct = default);
    Task<StreakDetailDto> JoinByInviteCodeAsync(string firebaseUid, string inviteCode, CancellationToken ct = default);
    Task<InviteDto> GetInviteAsync(string firebaseUid, Guid streakId, CancellationToken ct = default);
}
