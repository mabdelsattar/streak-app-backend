using Microsoft.EntityFrameworkCore;
using StreakPlatform.Application.Interfaces;
using StreakPlatform.Domain.Entities;

namespace StreakPlatform.Infrastructure.Persistence;

public class ParticipantRepository : IParticipantRepository
{
    private readonly AppDbContext _db;

    public ParticipantRepository(AppDbContext db) => _db = db;

    public Task<bool> ExistsAsync(Guid userId, Guid streakId, CancellationToken ct = default) =>
        _db.Participants.AnyAsync(p => p.UserId == userId && p.StreakId == streakId, ct);

    public async Task AddAsync(Participant participant, CancellationToken ct = default) =>
        await _db.Participants.AddAsync(participant, ct);

    public async Task<IReadOnlyList<Participant>> GetByStreakIdAsync(Guid streakId, CancellationToken ct = default) =>
        await _db.Participants
            .Include(p => p.User)
            .Where(p => p.StreakId == streakId)
            .OrderBy(p => p.JoinedAt)
            .ToListAsync(ct);
}
