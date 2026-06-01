namespace StreakPlatform.Application.Interfaces;

public record PaymentChargeResult(bool Success, string ReceiptId, string? FailureReason);

/// <summary>
/// Abstraction over the actual money-charging provider. The mock impl always succeeds.
/// Swap for Stripe / Apple IAP / Google Play by adding a new implementation.
/// </summary>
public interface IPaymentProvider
{
    Task<PaymentChargeResult> ChargeAsync(string packId, Guid userId, int amountUsdCents, CancellationToken ct = default);
}
