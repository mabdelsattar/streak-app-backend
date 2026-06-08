using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StreakPlatform.Application.Common;
using StreakPlatform.Application.Interfaces;

namespace StreakPlatform.Infrastructure.Services;

/// <summary>
/// Calls the Google Gemini REST API (Google AI Studio) to evaluate whether a
/// text check-in note is appropriate. Fails open on any transient error so an
/// API outage never blocks users.
/// </summary>
public class GeminiTextModerationService : ITextModerationService
{
    private readonly HttpClient _http;
    private readonly AppOptions _options;
    private readonly ILogger<GeminiTextModerationService> _logger;

    // The structured prompt tells Gemini exactly what JSON to return.
    private const string PromptTemplate = """
        You are a content moderator for a daily habit-tracking app.
        A user has submitted the following check-in note for their streak habit.
        Evaluate whether the content is appropriate.

        Reject (valid=false) if the note contains ANY of:
        - Profanity or offensive language
        - Hate speech or discrimination
        - Explicit or sexual content
        - Spam or meaningless gibberish (e.g. "aaaa", "123", random characters)
        - Completely unrelated content (advertising, URLs, code injections)

        Accept (valid=true) if the note is a genuine, human-written reflection about
        their daily habit or activity, even if brief (e.g. "did it", "30 min run", "✓").

        Respond with ONLY this JSON structure, no markdown, no extra text:
        {"valid": true, "reason": null}
        or
        {"valid": false, "reason": "short explanation in one sentence"}

        Note to evaluate: "{NOTE}"
        """;

    public GeminiTextModerationService(
        HttpClient http,
        IOptions<AppOptions> options,
        ILogger<GeminiTextModerationService> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ModerationResult> ModerateAsync(string text, CancellationToken ct = default)
    {
        var opts = _options.TextValidation;

        if (string.IsNullOrWhiteSpace(opts.GeminiApiKey))
        {
            _logger.LogWarning("Gemini API key is not configured. Skipping moderation (fail open).");
            return new ModerationResult(true, null);
        }

        try
        {
            var prompt = PromptTemplate.Replace("{NOTE}", EscapeForPrompt(text));
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{opts.GeminiModel}:generateContent?key={opts.GeminiApiKey}";

            var requestBody = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                },
                generationConfig = new
                {
                    responseMimeType = "application/json",
                    maxOutputTokens = 150,
                    temperature = 0.1   // low temperature = deterministic classification
                }
            };

            using var response = await _http.PostAsJsonAsync(url, requestBody, ct);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Gemini returned {Status}: {Body}. Failing open.", response.StatusCode, body);
                return new ModerationResult(true, null);
            }

            var raw = await response.Content.ReadAsStringAsync(ct);
            return ParseGeminiResponse(raw);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning("Gemini request timed out. Failing open.");
            return new ModerationResult(true, null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unexpected error calling Gemini. Failing open.");
            return new ModerationResult(true, null);
        }
    }

    /// <summary>
    /// Extracts and parses the JSON verdict from the Gemini response envelope.
    /// Returns fail-open (IsValid=true) if parsing fails.
    /// </summary>
    private ModerationResult ParseGeminiResponse(string rawResponse)
    {
        try
        {
            using var doc = JsonDocument.Parse(rawResponse);
            // Navigate: candidates[0].content.parts[0].text
            var verdictText = doc
                .RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? "";

            using var verdictDoc = JsonDocument.Parse(verdictText.Trim());
            var root = verdictDoc.RootElement;

            var isValid = root.GetProperty("valid").GetBoolean();
            string? reason = null;
            if (!isValid && root.TryGetProperty("reason", out var reasonEl))
                reason = reasonEl.GetString();

            return new ModerationResult(isValid, reason);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not parse Gemini verdict from response. Failing open. Raw: {Raw}",
                rawResponse.Length > 500 ? rawResponse[..500] : rawResponse);
            return new ModerationResult(true, null);
        }
    }

    /// <summary>Escapes user text so it can't break the prompt or inject instructions.</summary>
    private static string EscapeForPrompt(string text)
        => text.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ").Replace("\r", "");
}
