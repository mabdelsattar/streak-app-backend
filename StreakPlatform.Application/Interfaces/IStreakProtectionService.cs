using StreakPlatform.Application.DTOs;

namespace StreakPlatform.Application.Interfaces;

public interface IStreakProtectionService
{
    Task<ProtectionDto> ActivateAsync(string firebaseUid, Guid streakId, CancellationToken ct = default);
    Task CancelAsync(string firebaseUid, Guid streakId, CancellationToken ct = default);
    Task<IReadOnlyList<ProtectionDto>> GetMyPendingAsync(string firebaseUid, CancellationToken ct = default);
    Task<RestoreResultDto> RestoreAsync(string firebaseUid, Guid streakId, CancellationToken ct = default);
}
