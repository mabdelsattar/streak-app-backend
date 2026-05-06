using StreakPlatform.Application.DTOs;
using StreakPlatform.Domain.Entities;

namespace StreakPlatform.Application.Interfaces;

public interface IPointsService
{
    Task<PointsBalanceDto> GetBalanceAsync(string firebaseUid, CancellationToken ct = default);
    Task<IReadOnlyList<PointsTransactionDto>> GetTransactionsAsync(string firebaseUid, int take, CancellationToken ct = default);

    /// <summary>
    /// Adjusts the user's balance and writes a PointsTransaction in the same SaveChanges call.
    /// Caller must call SaveChangesAsync (via IUnitOfWork) afterwards if not awaiting persistence here.
    /// Returns the user's new balance.
    /// </summary>
    Task<int> AwardAsync(Guid userId, int delta, PointsTransactionReason reason,
        Guid? relatedStreakId = null, Guid? relatedProtectionId = null,
        CancellationToken ct = default);
}
