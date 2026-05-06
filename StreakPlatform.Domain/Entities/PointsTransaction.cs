namespace StreakPlatform.Domain.Entities;

public class PointsTransaction
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public int Delta { get; set; }
    public PointsTransactionReason Reason { get; set; }
    public Guid? RelatedStreakId { get; set; }
    public Guid? RelatedProtectionId { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
}
