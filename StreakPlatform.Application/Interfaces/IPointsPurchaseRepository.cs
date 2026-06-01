using StreakPlatform.Domain.Entities;

namespace StreakPlatform.Application.Interfaces;

public interface IPointsPurchaseRepository
{
    Task AddAsync(PointsPurchase purchase, CancellationToken ct = default);
    Task<IReadOnlyList<PointsPurchase>> GetForUserAsync(Guid userId, int take, CancellationToken ct = default);
}
