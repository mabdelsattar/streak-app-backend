namespace StreakPlatform.Application.Common;

public class AppOptions
{
    public string PublicBaseUrl { get; set; } = "http://localhost:4200";
    public string InvitePath { get; set; } = "/streaks/join";

    // Points & protection economy
    public int PointsPerCheckIn { get; set; } = 10;
    public int ProtectionCost { get; set; } = 50;
    public int StartingPointsBalance { get; set; } = 100;
    public int RestoreWindowHours { get; set; } = 24;

    // Media uploads
    public long MaxMediaSizeBytes { get; set; } = 5 * 1024 * 1024;
    public string[] AllowedMediaContentTypes { get; set; } = new[]
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };
    public string MediaStorageDirectory { get; set; } = "Media";
    public string MediaPublicPath { get; set; } = "/media";
}
