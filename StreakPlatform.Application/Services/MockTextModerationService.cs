using StreakPlatform.Application.Interfaces;

namespace StreakPlatform.Application.Services;

/// <summary>
/// No-op text moderation — always approves content.
/// Used during local development when no Gemini API key is configured.
/// </summary>
public class MockTextModerationService : ITextModerationService
{
    public Task<ModerationResult> ModerateAsync(string text, CancellationToken ct = default)
        => Task.FromResult(new ModerationResult(true, null));
}
