using StreakPlatform.Domain.Entities;

namespace StreakPlatform.Application.Interfaces;

public interface ICheckInRepository
{
    Task<bool> ExistsAsync(Guid userId, Guid streakId, DateOnly date, CancellationToken ct = default);
    Task AddAsync(CheckIn checkIn, CancellationToken ct = default);
    Task<IReadOnlyList<DateOnly>> GetUserDatesAsync(Guid userId, Guid streakId, CancellationToken ct = default);
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<DateOnly>>> GetDatesByUsersAsync(
        IEnumerable<Guid> userIds, Guid streakId, CancellationToken ct = default);
    Task<IReadOnlyList<Guid>> GetUsersCheckedInOnDateAsync(Guid streakId, DateOnly date, CancellationToken ct = default);

    /// <summary>
    /// Returns recent check-ins (with note or media) for a streak, joined with the user's display name.
    /// </summary>
    Task<IReadOnlyList<(CheckIn CheckIn, string DisplayName)>> GetFeedAsync(
        Guid streakId, int take, int skip, CancellationToken ct = default);
}
