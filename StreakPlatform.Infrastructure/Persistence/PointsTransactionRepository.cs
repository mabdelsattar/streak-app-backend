using Microsoft.EntityFrameworkCore;
using StreakPlatform.Application.Interfaces;
using StreakPlatform.Domain.Entities;

namespace StreakPlatform.Infrastructure.Persistence;

public class PointsTransactionRepository : IPointsTransactionRepository
{
    private readonly AppDbContext _db;

    public PointsTransactionRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(PointsTransaction tx, CancellationToken ct = default) =>
        await _db.PointsTransactions.AddAsync(tx, ct);

    public async Task<IReadOnlyList<PointsTransaction>> GetForUserAsync(Guid userId, int take, CancellationToken ct = default) =>
        await _db.PointsTransactions
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .Take(take)
            .ToListAsync(ct);
}
