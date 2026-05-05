namespace StreakPlatform.Domain.Entities;

public class Participant
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid StreakId { get; set; }
    public DateTime JoinedAt { get; set; }

    public User User { get; set; } = null!;
    public Streak Streak { get; set; } = null!;
}
