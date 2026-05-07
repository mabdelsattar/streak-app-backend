using Microsoft.EntityFrameworkCore;
using StreakPlatform.Application.Interfaces;
using StreakPlatform.Domain.Entities;

namespace StreakPlatform.Infrastructure.Persistence;

public class ReactionRepository : IReactionRepository
{
    private readonly AppDbContext _db;

    public ReactionRepository(AppDbContext db) => _db = db;

    public Task<CheckInReaction?> GetAsync(Guid checkInId, Guid reactorUserId, CancellationToken ct = default) =>
        _db.CheckInReactions.FirstOrDefaultAsync(
            r => r.CheckInId == checkInId && r.ReactorUserId == reactorUserId, ct);

    public async Task AddAsync(CheckInReaction reaction, CancellationToken ct = default) =>
        await _db.CheckInReactions.AddAsync(reaction, ct);

    public void Remove(CheckInReaction reaction) =>
        _db.CheckInReactions.Remove(reaction);

    public async Task<(int Likes, int Dislikes)> CountAsync(Guid checkInId, CancellationToken ct = default)
    {
        var groups = await _db.CheckInReactions
            .Where(r => r.CheckInId == checkInId)
            .GroupBy(r => r.Type)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(ct);
        var likes = groups.FirstOrDefault(x => x.Key == ReactionType.Like)?.Count ?? 0;
        var dislikes = groups.FirstOrDefault(x => x.Key == ReactionType.Dislike)?.Count ?? 0;
        return (likes, dislikes);
    }

    public async Task<IReadOnlyDictionary<Guid, (int Likes, int Dislikes)>> CountByCheckInAsync(IEnumerable<Guid> checkInIds, CancellationToken ct = default)
    {
        var ids = checkInIds.ToList();
        if (ids.Count == 0)
            return new Dictionary<Guid, (int, int)>();

        var rows = await _db.CheckInReactions
            .Where(r => ids.Contains(r.CheckInId))
            .GroupBy(r => new { r.CheckInId, r.Type })
            .Select(g => new { g.Key.CheckInId, g.Key.Type, Count = g.Count() })
            .ToListAsync(ct);

        return rows
            .GroupBy(r => r.CheckInId)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var likes = g.FirstOrDefault(x => x.Type == ReactionType.Like)?.Count ?? 0;
                    var dislikes = g.FirstOrDefault(x => x.Type == ReactionType.Dislike)?.Count ?? 0;
                    return (likes, dislikes);
                });
    }

    public async Task<IReadOnlyDictionary<Guid, ReactionType>> GetMyReactionsByCheckInAsync(IEnumerable<Guid> checkInIds, Guid reactorUserId, CancellationToken ct = default)
    {
        var ids = checkInIds.ToList();
        if (ids.Count == 0)
            return new Dictionary<Guid, ReactionType>();

        var rows = await _db.CheckInReactions
            .Where(r => r.ReactorUserId == reactorUserId && ids.Contains(r.CheckInId))
            .Select(r => new { r.CheckInId, r.Type })
            .ToListAsync(ct);

        return rows.ToDictionary(x => x.CheckInId, x => x.Type);
    }
}
