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
}
