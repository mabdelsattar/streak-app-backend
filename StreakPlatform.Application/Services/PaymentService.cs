using Microsoft.Extensions.Options;
using StreakPlatform.Application.Common;
using StreakPlatform.Application.DTOs;
using StreakPlatform.Application.Interfaces;
using StreakPlatform.Domain.Entities;

namespace StreakPlatform.Application.Services;

public class PaymentService : IPaymentService
{
    private readonly IUserRepository _users;
    private readonly IPaymentProvider _provider;
    private readonly IPointsPurchaseRepository _purchases;
    private readonly IPointsService _points;
    private readonly IUnitOfWork _uow;
    private readonly AppOptions _options;

    public PaymentService(
        IUserRepository users,
        IPaymentProvider provider,
        IPointsPurchaseRepository purchases,
        IPointsService points,
        IUnitOfWork uow,
        IOptions<AppOptions> options)
    {
        _users = users;
        _provider = provider;
        _purchases = purchases;
        _points = points;
        _uow = uow;
        _options = options.Value;
    }

    public IReadOnlyList<PointsPackDto> GetPacks() =>
        _options.Payments.Packs
            .Select(p => new PointsPackDto(p.Id, p.Points, p.PriceUsdCents, p.Label, p.IsPopular))
            .ToList();

    public async Task<PurchaseResultDto> PurchaseAsync(string firebaseUid, string packId, CancellationToken ct = default)
    {
        var user = await _users.GetByFirebaseUidAsync(firebaseUid, ct)
            ?? throw new NotFoundException("User not initialized.");

        var pack = _options.Payments.Packs.FirstOrDefault(p => p.Id == packId)
            ?? throw new NotFoundException($"pack_not_found: {packId}");

        var charge = await _provider.ChargeAsync(packId, user.Id, pack.PriceUsdCents, ct);
        if (!charge.Success)
            throw new ConflictException(charge.FailureReason ?? "Payment declined.");

        var purchase = new PointsPurchase
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            PackId = pack.Id,
            PointsAdded = pack.Points,
            AmountUsdCents = pack.PriceUsdCents,
            Provider = _options.Payments.Provider.ToLowerInvariant(),
            ExternalReceiptId = charge.ReceiptId,
            CreatedAt = DateTime.UtcNow
        };
        await _purchases.AddAsync(purchase, ct);

        var newBalance = await _points.AwardAsync(user.Id, pack.Points,
            PointsTransactionReason.Purchase, null, null, ct);

        await _uow.SaveChangesAsync(ct);

        return new PurchaseResultDto(purchase.Id, pack.Id, pack.Points, newBalance, charge.ReceiptId, purchase.CreatedAt);
    }

    public async Task<IReadOnlyList<PurchaseResultDto>> GetHistoryAsync(string firebaseUid, int take, CancellationToken ct = default)
    {
        var user = await _users.GetByFirebaseUidAsync(firebaseUid, ct)
            ?? throw new NotFoundException("User not initialized.");
        var rows = await _purchases.GetForUserAsync(user.Id, Math.Clamp(take, 1, 200), ct);
        return rows.Select(r => new PurchaseResultDto(
            r.Id, r.PackId, r.PointsAdded, 0 /* unused on history */, r.ExternalReceiptId, r.CreatedAt)).ToList();
    }
}
