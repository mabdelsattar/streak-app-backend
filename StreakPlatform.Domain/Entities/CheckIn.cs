namespace StreakPlatform.Domain.Entities;

public class CheckIn
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid StreakId { get; set; }
    public DateOnly Date { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Note { get; set; }
    public string? MediaUrl { get; set; }
    public string? MediaContentType { get; set; }
    public int? MediaDurationSeconds { get; set; }

    public User User { get; set; } = null!;
    public Streak Streak { get; set; } = null!;
    public ICollection<CheckInReaction> Reactions { get; set; } = new List<CheckInReaction>();
}
