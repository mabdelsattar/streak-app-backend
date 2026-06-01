namespace StreakPlatform.Application.DTOs;

public record PurchaseResultDto(
    Guid PurchaseId,
    string PackId,
    int PointsAdded,
    int NewBalance,
    string ReceiptId,
    DateTime CreatedAt);
