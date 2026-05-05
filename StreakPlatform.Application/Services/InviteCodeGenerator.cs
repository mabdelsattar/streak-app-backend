using System.Security.Cryptography;
using StreakPlatform.Application.Interfaces;

namespace StreakPlatform.Application.Services;

public class InviteCodeGenerator
{
    // Excludes ambiguous chars (0, O, 1, I, L) for shareability.
    private const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
    private const int Length = 8;

    private readonly IStreakRepository _streaks;

    public InviteCodeGenerator(IStreakRepository streaks) => _streaks = streaks;

    public async Task<string> GenerateUniqueAsync(CancellationToken ct = default)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var code = NewCode();
            if (!await _streaks.InviteCodeExistsAsync(code, ct))
                return code;
        }
        throw new InvalidOperationException("Unable to generate a unique invite code after 10 attempts.");
    }

    private static string NewCode()
    {
        Span<byte> bytes = stackalloc byte[Length];
        RandomNumberGenerator.Fill(bytes);
        Span<char> chars = stackalloc char[Length];
        for (var i = 0; i < Length; i++)
            chars[i] = Alphabet[bytes[i] % Alphabet.Length];
        return new string(chars);
    }
}
