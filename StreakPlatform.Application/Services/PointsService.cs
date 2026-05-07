using StreakPlatform.Application.Common;
using StreakPlatform.Application.DTOs;
using StreakPlatform.Application.Interfaces;
using StreakPlatform.Domain.Entities;

namespace StreakPlatform.Application.Services;

public class PointsService : IPointsService
{
    private readonly IUserRepository _users;
    private readonly IPointsTransactionRepository _txs;

    public PointsService(IUserRepository users, IPointsTransactionRepository txs)
    {
        _users = users;
        _txs = txs;
    }

    public async Task<PointsBalanceDto> GetBalanceAsync(string firebaseUid, CancellationToken ct = default)
    {
        var user = await _users.GetByFirebaseUidAsync(firebaseUid, ct)
            ?? throw new NotFoundException("User not initialized.");
        return new PointsBalanceDto(user.PointsBalance);
    }

    public async Task<IReadOnlyList<PointsTransactionDto>> GetTransactionsAsync(string firebaseUid, int take, CancellationToken ct = default)
    {
        var user = await _users.GetByFirebaseUidAsync(firebaseUid, ct)
            ?? throw new NotFoundException("User not initialized.");
        var rows = await _txs.GetForUserAsync(user.Id, Math.Clamp(take, 1, 200), ct);
        return rows.Select(t => new PointsTransactionDto(
            t.Id,
            t.Delta,
            t.Reason.ToString(),
            t.RelatedStreakId,
            t.CreatedAt)).ToList();
    }

    /// <summary>
    /// Adjusts the user's balance and writes a PointsTransaction.
    /// Balance is CLAMPED at 0 — if the requested delta would push it negative,
    /// the recorded transaction reflects the actual delta applied (smaller magnitude),
    /// not the requested one. Caller still gets the new balance.
    /// </summary>
    public async Task<int> AwardAsync(Guid userId, int delta, PointsTransactionReason reason,
        Guid? relatedStreakId = null, Guid? relatedProtectionId = null,
        CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException("User not found.");

        var requested = user.PointsBalance + delta;
        var newBalance = requested < 0 ? 0 : requested;
        var actualDelta = newBalance - user.PointsBalance;

        user.PointsBalance = newBalance;
        user.UpdatedAt = DateTime.UtcNow;

        if (actualDelta != 0)
        {
            await _txs.AddAsync(new PointsTransaction
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Delta = actualDelta,
                Reason = reason,
                RelatedStreakId = relatedStreakId,
                RelatedProtectionId = relatedProtectionId,
                CreatedAt = DateTime.UtcNow
            }, ct);
        }

        return newBalance;
    }
}
