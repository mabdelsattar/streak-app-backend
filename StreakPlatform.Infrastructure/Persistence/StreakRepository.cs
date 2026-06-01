using Microsoft.EntityFrameworkCore;
using StreakPlatform.Application.Interfaces;
using StreakPlatform.Domain.Entities;

namespace StreakPlatform.Infrastructure.Persistence;

public class StreakRepository : IStreakRepository
{
    private readonly AppDbContext _db;

    public StreakRepository(AppDbContext db) => _db = db;

    public Task<Streak?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Streaks.FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<Streak?> GetByIdWithParticipantsAsync(Guid id, CancellationToken ct = default) =>
        _db.Streaks
            .Include(s => s.Participants).ThenInclude(p => p.User)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<Streak?> GetByInviteCodeAsync(string inviteCode, CancellationToken ct = default) =>
        _db.Streaks.FirstOrDefaultAsync(s => s.InviteCode == inviteCode, ct);

    public Task<bool> InviteCodeExistsAsync(string inviteCode, CancellationToken ct = default) =>
        _db.Streaks.AnyAsync(s => s.InviteCode == inviteCode, ct);

    public async Task<IReadOnlyList<Streak>> GetForUserAsync(Guid userId, CancellationToken ct = default) =>
        await _db.Streaks
            .Include(s => s.Participants)
            .Where(s => s.Participants.Any(p => p.UserId == userId))
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(ct);

    public async Task AddAsync(Streak streak, CancellationToken ct = default) =>
        await _db.Streaks.AddAsync(streak, ct);

    public async Task<IReadOnlyList<Streak>> GetPublicForDiscoveryAsync(
        Guid currentUserId, int take, int skip, string? search, CancellationToken ct = default)
    {
        var q = _db.Streaks
            .Include(s => s.Participants)
            .Include(s => s.Creator)
            .Where(s => s.IsPublic
                        && !s.Participants.Any(p => p.UserId == currentUserId));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            q = q.Where(s => EF.Functions.Like(s.Name, pattern));
        }

        return await q
            .OrderByDescending(s => s.Participants.Count)
            .ThenByDescending(s => s.CreatedAt)
            .Skip(Math.Max(skip, 0))
            .Take(Math.Clamp(take, 1, 100))
            .ToListAsync(ct);
    }
}
