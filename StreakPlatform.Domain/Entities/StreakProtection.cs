namespace StreakPlatform.Domain.Entities;

public class StreakProtection
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid StreakId { get; set; }
    public ProtectionStatus Status { get; set; }
    public int PointsCost { get; set; }
    public DateTime ScheduledAt { get; set; }
    public DateOnly? AppliedToDate { get; set; }
    public DateTime? AppliedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
    public Streak Streak { get; set; } = null!;
}
