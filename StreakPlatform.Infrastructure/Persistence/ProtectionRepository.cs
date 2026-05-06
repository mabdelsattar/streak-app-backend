using Microsoft.EntityFrameworkCore;
using StreakPlatform.Application.Interfaces;
using StreakPlatform.Domain.Entities;

namespace StreakPlatform.Infrastructure.Persistence;

public class ProtectionRepository : IProtectionRepository
{
    private readonly AppDbContext _db;

    public ProtectionRepository(AppDbContext db) => _db = db;

    public Task<StreakProtection?> GetPendingAsync(Guid userId, Guid streakId, CancellationToken ct = default) =>
        _db.StreakProtections.FirstOrDefaultAsync(
            p => p.UserId == userId && p.StreakId == streakId && p.Status == ProtectionStatus.Pending, ct);

    public Task<StreakProtection?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.StreakProtections.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<StreakProtection>> GetPendingForUserAsync(Guid userId, CancellationToken ct = default) =>
        await _db.StreakProtections
            .Where(p => p.UserId == userId && p.Status == ProtectionStatus.Pending)
            .OrderByDescending(p => p.ScheduledAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<DateOnly>> GetUsedDatesAsync(Guid userId, Guid streakId, CancellationToken ct = default) =>
        await _db.StreakProtections
            .Where(p => p.UserId == userId && p.StreakId == streakId && p.Status == ProtectionStatus.Used && p.AppliedToDate != null)
            .Select(p => p.AppliedToDate!.Value)
            .ToListAsync(ct);

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<DateOnly>>> GetUsedDatesByUsersAsync(
        IEnumerable<Guid> userIds, Guid streakId, CancellationToken ct = default)
    {
        var ids = userIds.ToList();
        if (ids.Count == 0)
            return new Dictionary<Guid, IReadOnlyList<DateOnly>>();

        var rows = await _db.StreakProtections
            .Where(p => p.StreakId == streakId
                        && p.Status == ProtectionStatus.Used
                        && p.AppliedToDate != null
                        && ids.Contains(p.UserId))
            .Select(p => new { p.UserId, Date = p.AppliedToDate!.Value })
            .ToListAsync(ct);

        return rows
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<DateOnly>)g.Select(x => x.Date).ToList());
    }

    public async Task AddAsync(StreakProtection protection, CancellationToken ct = default) =>
        await _db.StreakProtections.AddAsync(protection, ct);
}
