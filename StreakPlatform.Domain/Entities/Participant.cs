namespace StreakPlatform.Domain.Entities;

public class Participant
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid StreakId { get; set; }
    public DateTime JoinedAt { get; set; }

    /// <summary>
    /// False = the user "left" this streak (soft kick). They no longer appear in active rosters
    /// or feeds, and don't owe any debt. The row is retained for history.
    /// </summary>
    public bool IsActive { get; set; } = true;

    public DateTime? InactiveAt { get; set; }
    public string? InactiveReason { get; set; }

    public User User { get; set; } = null!;
    public Streak Streak { get; set; } = null!;
}
