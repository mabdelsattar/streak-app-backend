using StreakPlatform.Domain.Entities;

namespace StreakPlatform.Application.Interfaces;

public interface IPointsTransactionRepository
{
    Task AddAsync(PointsTransaction tx, CancellationToken ct = default);
    Task<IReadOnlyList<PointsTransaction>> GetForUserAsync(Guid userId, int take, CancellationToken ct = default);
}
