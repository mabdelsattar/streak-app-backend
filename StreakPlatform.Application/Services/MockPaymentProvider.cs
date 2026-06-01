using StreakPlatform.Application.Interfaces;

namespace StreakPlatform.Application.Services;

/// <summary>
/// Always-succeeds mock. Generates a fake receipt id. Use this for local dev and demos.
/// Replace with Stripe / Apple IAP / Google Play by registering a different IPaymentProvider.
/// </summary>
public class MockPaymentProvider : IPaymentProvider
{
    public Task<PaymentChargeResult> ChargeAsync(string packId, Guid userId, int amountUsdCents, CancellationToken ct = default)
    {
        var receiptId = $"mock_{Guid.NewGuid():N}";
        return Task.FromResult(new PaymentChargeResult(true, receiptId, null));
    }
}
