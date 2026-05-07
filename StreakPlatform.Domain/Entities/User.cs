namespace StreakPlatform.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string FirebaseUid { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? DisplayName { get; set; }
    public int PointsBalance { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<Streak> CreatedStreaks { get; set; } = new List<Streak>();
    public ICollection<Participant> Participations { get; set; } = new List<Participant>();
    public ICollection<CheckIn> CheckIns { get; set; } = new List<CheckIn>();
    public ICollection<StreakProtection> Protections { get; set; } = new List<StreakProtection>();
    public ICollection<PointsTransaction> PointsTransactions { get; set; } = new List<PointsTransaction>();
    public ICollection<CheckInReaction> Reactions { get; set; } = new List<CheckInReaction>();
}
