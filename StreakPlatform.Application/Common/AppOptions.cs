namespace StreakPlatform.Application.Common;

public class AppOptions
{
    public string PublicBaseUrl { get; set; } = "http://localhost:4200";
    public string InvitePath { get; set; } = "/streaks/join";
}
