using StreakPlatform.Application.DTOs;

namespace StreakPlatform.Application.Interfaces;

public interface IPaymentService
{
    IReadOnlyList<PointsPackDto> GetPacks();
    Task<PurchaseResultDto> PurchaseAsync(string firebaseUid, string packId, CancellationToken ct = default);
    Task<IReadOnlyList<PurchaseResultDto>> GetHistoryAsync(string firebaseUid, int take, CancellationToken ct = default);
}
