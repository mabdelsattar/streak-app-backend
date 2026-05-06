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

    public async Task<int> AwardAsync(Guid userId, int delta, PointsTransactionReason reason,
        Guid? relatedStreakId = null, Guid? relatedProtectionId = null,
        CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException("User not found.");

        var newBalance = user.PointsBalance + delta;
        if (newBalance < 0)
            throw new ConflictException("Insufficient points.");

        user.PointsBalance = newBalance;
        user.UpdatedAt = DateTime.UtcNow;

        await _txs.AddAsync(new PointsTransaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Delta = delta,
            Reason = reason,
            RelatedStreakId = relatedStreakId,
            RelatedProtectionId = relatedProtectionId,
            CreatedAt = DateTime.UtcNow
        }, ct);

        return newBalance;
    }
}
