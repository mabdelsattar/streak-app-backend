namespace StreakPlatform.Domain.Entities;

public class PointsPurchase
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string PackId { get; set; } = null!;
    public int PointsAdded { get; set; }
    public int AmountUsdCents { get; set; }
    public string Provider { get; set; } = "mock";
    public string ExternalReceiptId { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
}
