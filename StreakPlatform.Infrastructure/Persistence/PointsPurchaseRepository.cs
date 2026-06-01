using Microsoft.EntityFrameworkCore;
using StreakPlatform.Application.Interfaces;
using StreakPlatform.Domain.Entities;

namespace StreakPlatform.Infrastructure.Persistence;

public class PointsPurchaseRepository : IPointsPurchaseRepository
{
    private readonly AppDbContext _db;

    public PointsPurchaseRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(PointsPurchase purchase, CancellationToken ct = default) =>
        await _db.PointsPurchases.AddAsync(purchase, ct);

    public async Task<IReadOnlyList<PointsPurchase>> GetForUserAsync(Guid userId, int take, CancellationToken ct = default) =>
        await _db.PointsPurchases
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .Take(take)
            .ToListAsync(ct);
}
