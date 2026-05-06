using StreakPlatform.Domain.Entities;

namespace StreakPlatform.Application.Interfaces;

public interface IProtectionRepository
{
    Task<StreakProtection?> GetPendingAsync(Guid userId, Guid streakId, CancellationToken ct = default);
    Task<StreakProtection?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<StreakProtection>> GetPendingForUserAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<DateOnly>> GetUsedDatesAsync(Guid userId, Guid streakId, CancellationToken ct = default);
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<DateOnly>>> GetUsedDatesByUsersAsync(
        IEnumerable<Guid> userIds, Guid streakId, CancellationToken ct = default);
    Task AddAsync(StreakProtection protection, CancellationToken ct = default);
}
