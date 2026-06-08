namespace StreakPlatform.Application.Interfaces;

/// <summary>Result returned by the text moderation provider.</summary>
/// <param name="IsValid">True if the content is acceptable; false if it should be rejected.</param>
/// <param name="Reason">Human-readable rejection reason shown to the user, or null when valid.</param>
public record ModerationResult(bool IsValid, string? Reason);

public interface ITextModerationService
{
    /// <summary>
    /// Evaluates whether the supplied text is appropriate for a streak check-in.
    /// Implementations should fail open (return IsValid=true) on transient errors so
    /// a provider outage never blocks users from checking in.
    /// </summary>
    Task<ModerationResult> ModerateAsync(string text, CancellationToken ct = default);
}
