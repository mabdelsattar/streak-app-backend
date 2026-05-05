using Microsoft.EntityFrameworkCore;
using StreakPlatform.Domain.Entities;

namespace StreakPlatform.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Streak> Streaks => Set<Streak>();
    public DbSet<Participant> Participants => Set<Participant>();
    public DbSet<CheckIn> CheckIns => Set<CheckIn>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.FirebaseUid).IsRequired().HasMaxLength(128);
            e.Property(x => x.Email).IsRequired().HasMaxLength(256);
            e.Property(x => x.DisplayName).HasMaxLength(60);
            e.HasIndex(x => x.FirebaseUid).IsUnique();
            e.HasIndex(x => x.Email).IsUnique();
        });

        b.Entity<Streak>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(100);
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.InviteCode).IsRequired().HasMaxLength(16);
            e.HasIndex(x => x.InviteCode).IsUnique();
            e.HasOne(x => x.Creator)
                .WithMany(u => u.CreatedStreaks)
                .HasForeignKey(x => x.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Participant>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.UserId, x.StreakId }).IsUnique();
            e.HasOne(x => x.User)
                .WithMany(u => u.Participations)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Streak)
                .WithMany(s => s.Participants)
                .HasForeignKey(x => x.StreakId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<CheckIn>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.UserId, x.StreakId, x.Date }).IsUnique();
            e.HasIndex(x => new { x.StreakId, x.Date });
            e.Property(x => x.MediaUrl).HasMaxLength(500);
            e.HasOne(x => x.User)
                .WithMany(u => u.CheckIns)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Streak)
                .WithMany(s => s.CheckIns)
                .HasForeignKey(x => x.StreakId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
