namespace StreakPlatform.Domain.Entities;

public class Streak
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public Guid CreatedBy { get; set; }
    public string InviteCode { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    public User Creator { get; set; } = null!;
    public ICollection<Participant> Participants { get; set; } = new List<Participant>();
    public ICollection<CheckIn> CheckIns { get; set; } = new List<CheckIn>();
}
