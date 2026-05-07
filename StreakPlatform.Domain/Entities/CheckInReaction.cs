namespace StreakPlatform.Domain.Entities;

public class CheckInReaction
{
    public Guid Id { get; set; }
    public Guid CheckInId { get; set; }
    public Guid ReactorUserId { get; set; }
    public ReactionType Type { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public CheckIn CheckIn { get; set; } = null!;
    public User Reactor { get; set; } = null!;
}
