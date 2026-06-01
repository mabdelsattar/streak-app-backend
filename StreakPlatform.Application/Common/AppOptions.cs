namespace StreakPlatform.Application.Common;

public class AppOptions
{
    public string PublicBaseUrl { get; set; } = "http://localhost:4200";
    public string InvitePath { get; set; } = "/streaks/join";

    // Points & protection economy
    public int PointsPerCheckIn { get; set; } = 10;
    public int ProtectionCost { get; set; } = 50;           // legacy — kept for migration safety
    public int StartingPointsBalance { get; set; } = 100;
    public int RestoreWindowHours { get; set; } = 24;       // legacy — no longer used

    // Forced missed-day recovery (replaces Protection + Restore)
    public int MissedDayRecoveryCost { get; set; } = 10;    // per missed day

    // Reactions
    public int ReactionLikePoints { get; set; } = 5;
    public int ReactionDislikePoints { get; set; } = 5;

    // Discovery / public streaks
    public int PublicStreakJoinCost { get; set; } = 0;   // 0 = free; raise later if needed
    public int PublicListDefaultTake { get; set; } = 20;

    // Payments (mock by default)
    public PaymentsOptions Payments { get; set; } = new();

    // Media
    public long MaxMediaSizeBytes { get; set; } = 5 * 1024 * 1024;       // 5 MB images (default)
    public long MaxAudioSizeBytes { get; set; } = 10 * 1024 * 1024;      // 10 MB audio
    public long MaxVideoSizeBytes { get; set; } = 50 * 1024 * 1024;      // 50 MB video
    public string[] AllowedMediaContentTypes { get; set; } = new[]
    {
        "image/jpeg", "image/png", "image/webp",
        "audio/webm", "audio/ogg", "audio/mp4",
        "video/webm", "video/mp4"
    };
    public string MediaStorageDirectory { get; set; } = "Media";
    public string MediaPublicPath { get; set; } = "/media";

    // Storage provider
    public MediaStorageOptions MediaStorage { get; set; } = new();
    public GcsOptions Gcs { get; set; } = new();
}

public class MediaStorageOptions
{
    public string Provider { get; set; } = "Local";   // "Local" | "Gcs"
}

public class GcsOptions
{
    public string BucketName { get; set; } = "";
    public string ProjectId { get; set; } = "";
    public string CredentialsPath { get; set; } = "";
    public int UploadUrlTtlMinutes { get; set; } = 10;
    public string PublicBaseUrl { get; set; } = "https://storage.googleapis.com";
}

public class PaymentsOptions
{
    public string Provider { get; set; } = "Mock";   // "Mock" — Stripe / Apple / Google to come
    public List<PointsPackConfig> Packs { get; set; } = new()
    {
        new() { Id = "pack_100",  Points = 100,  PriceUsdCents = 99,  Label = "Starter",      IsPopular = false },
        new() { Id = "pack_500",  Points = 500,  PriceUsdCents = 399, Label = "Most popular", IsPopular = true },
        new() { Id = "pack_1500", Points = 1500, PriceUsdCents = 999, Label = "Best value",   IsPopular = false }
    };
}

public class PointsPackConfig
{
    public string Id { get; set; } = null!;
    public int Points { get; set; }
    public int PriceUsdCents { get; set; }
    public string Label { get; set; } = "";
    public bool IsPopular { get; set; }
}
