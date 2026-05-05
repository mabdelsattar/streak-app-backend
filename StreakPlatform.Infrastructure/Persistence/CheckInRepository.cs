using Microsoft.EntityFrameworkCore;
using StreakPlatform.Application.Interfaces;
using StreakPlatform.Domain.Entities;

namespace StreakPlatform.Infrastructure.Persistence;

public class CheckInRepository : ICheckInRepository
{
    private readonly AppDbContext _db;

    public CheckInRepository(AppDbContext db) => _db = db;

    public Task<bool> ExistsAsync(Guid userId, Guid streakId, DateOnly date, CancellationToken ct = default) =>
        _db.CheckIns.AnyAsync(c => c.UserId == userId && c.StreakId == streakId && c.Date == date, ct);

    public async Task AddAsync(CheckIn checkIn, CancellationToken ct = default) =>
        await _db.CheckIns.AddAsync(checkIn, ct);

    public async Task<IReadOnlyList<DateOnly>> GetUserDatesAsync(Guid userId, Guid streakId, CancellationToken ct = default) =>
        await _db.CheckIns
            .Where(c => c.UserId == userId && c.StreakId == streakId)
            .OrderByDescending(c => c.Date)
            .Select(c => c.Date)
            .ToListAsync(ct);

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<DateOnly>>> GetDatesByUsersAsync(
        IEnumerable<Guid> userIds, Guid streakId, CancellationToken ct = default)
    {
        var ids = userIds.ToList();
        if (ids.Count == 0)
            return new Dictionary<Guid, IReadOnlyList<DateOnly>>();

        var rows = await _db.CheckIns
            .Where(c => c.StreakId == streakId && ids.Contains(c.UserId))
            .Select(c => new { c.UserId, c.Date })
            .ToListAsync(ct);

        return rows
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<DateOnly>)g.Select(x => x.Date).OrderByDescending(d => d).ToList());
    }

    public async Task<IReadOnlyList<Guid>> GetUsersCheckedInOnDateAsync(Guid streakId, DateOnly date, CancellationToken ct = default) =>
        await _db.CheckIns
            .Where(c => c.StreakId == streakId && c.Date == date)
            .Select(c => c.UserId)
            .ToListAsync(ct);
}
