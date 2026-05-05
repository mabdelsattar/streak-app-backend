using StreakPlatform.Domain.Entities;

namespace StreakPlatform.Application.Interfaces;

public interface ICheckInRepository
{
    Task<bool> ExistsAsync(Guid userId, Guid streakId, DateOnly date, CancellationToken ct = default);
    Task AddAsync(CheckIn checkIn, CancellationToken ct = default);
    /// <summary>Returns dates (DESC) for one user's check-ins on a streak.</summary>
    Task<IReadOnlyList<DateOnly>> GetUserDatesAsync(Guid userId, Guid streakId, CancellationToken ct = default);
    /// <summary>Returns dates per (UserId, StreakId) for many users on the same streak.</summary>
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<DateOnly>>> GetDatesByUsersAsync(
        IEnumerable<Guid> userIds, Guid streakId, CancellationToken ct = default);
    Task<IReadOnlyList<Guid>> GetUsersCheckedInOnDateAsync(Guid streakId, DateOnly date, CancellationToken ct = default);
}
