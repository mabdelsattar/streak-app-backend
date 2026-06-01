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
    public DbSet<StreakProtection> StreakProtections => Set<StreakProtection>();
    public DbSet<PointsTransaction> PointsTransactions => Set<PointsTransaction>();
    public DbSet<CheckInReaction> CheckInReactions => Set<CheckInReaction>();
    public DbSet<PointsPurchase> PointsPurchases => Set<PointsPurchase>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.FirebaseUid).IsRequired().HasMaxLength(128);
            e.Property(x => x.Email).IsRequired().HasMaxLength(256);
            e.Property(x => x.DisplayName).HasMaxLength(60);
            e.Property(x => x.PointsBalance).HasDefaultValue(100);
            e.HasIndex(x => x.FirebaseUid).IsUnique();
            e.HasIndex(x => x.Email).IsUnique();
        });

        b.Entity<Streak>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(100);
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.InviteCode).IsRequired().HasMaxLength(16);
            e.Property(x => x.CheckInType).HasDefaultValue(CheckInType.Action);
            e.Property(x => x.CheckInButtonLabel).HasMaxLength(40);
            e.Property(x => x.IsPublic).HasDefaultValue(false);
            e.HasIndex(x => x.InviteCode).IsUnique();
            e.HasIndex(x => new { x.IsPublic, x.CreatedAt });
            e.HasOne(x => x.Creator)
                .WithMany(u => u.CreatedStreaks)
                .HasForeignKey(x => x.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Participant>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.UserId, x.StreakId }).IsUnique();
            e.HasIndex(x => new { x.StreakId, x.IsActive });
            e.Property(x => x.IsActive).HasDefaultValue(true);
            e.Property(x => x.InactiveReason).HasMaxLength(40);
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
            e.HasIndex(x => new { x.StreakId, x.CreatedAt });
            e.Property(x => x.Note).HasMaxLength(500);
            e.Property(x => x.MediaUrl).HasMaxLength(500);
            e.Property(x => x.MediaContentType).HasMaxLength(100);
            e.HasOne(x => x.User)
                .WithMany(u => u.CheckIns)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Streak)
                .WithMany(s => s.CheckIns)
                .HasForeignKey(x => x.StreakId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<StreakProtection>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.UserId, x.StreakId, x.Status })
                .HasFilter("[Status] = 0")
                .IsUnique()
                .HasDatabaseName("IX_StreakProtections_OnePendingPerUserStreak");
            e.HasIndex(x => new { x.UserId, x.StreakId, x.AppliedToDate });
            e.HasOne(x => x.User)
                .WithMany(u => u.Protections)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Streak)
                .WithMany()
                .HasForeignKey(x => x.StreakId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<PointsTransaction>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.UserId, x.CreatedAt });
            e.HasOne(x => x.User)
                .WithMany(u => u.PointsTransactions)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<CheckInReaction>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.CheckInId, x.ReactorUserId }).IsUnique();
            e.HasIndex(x => x.CheckInId);
            e.HasOne(x => x.CheckIn)
                .WithMany(c => c.Reactions)
                .HasForeignKey(x => x.CheckInId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Reactor)
                .WithMany(u => u.Reactions)
                .HasForeignKey(x => x.ReactorUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<PointsPurchase>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.PackId).IsRequired().HasMaxLength(40);
            e.Property(x => x.Provider).IsRequired().HasMaxLength(20);
            e.Property(x => x.ExternalReceiptId).IsRequired().HasMaxLength(100);
            e.HasIndex(x => new { x.UserId, x.CreatedAt });
            e.HasIndex(x => x.ExternalReceiptId);
            e.HasOne(x => x.User)
                .WithMany(u => u.Purchases)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
