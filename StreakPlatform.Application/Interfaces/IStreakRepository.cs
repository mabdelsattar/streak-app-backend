using StreakPlatform.Domain.Entities;

namespace StreakPlatform.Application.Interfaces;

public interface IStreakRepository
{
    Task<Streak?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Streak?> GetByIdWithParticipantsAsync(Guid id, CancellationToken ct = default);
    Task<Streak?> GetByInviteCodeAsync(string inviteCode, CancellationToken ct = default);
    Task<bool> InviteCodeExistsAsync(string inviteCode, CancellationToken ct = default);
    Task<IReadOnlyList<Streak>> GetForUserAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(Streak streak, CancellationToken ct = default);

    /// <summary>
    /// Returns public streaks the user is NOT already a participant of.
    /// Search is a case-insensitive contains-match on streak name (optional).
    /// </summary>
    Task<IReadOnlyList<Streak>> GetPublicForDiscoveryAsync(
        Guid currentUserId, int take, int skip, string? search, CancellationToken ct = default);
}
