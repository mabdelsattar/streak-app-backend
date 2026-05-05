using StreakPlatform.Application.DTOs;

namespace StreakPlatform.Application.Interfaces;

public interface IUserService
{
    Task<UserProfileDto> InitializeAsync(string firebaseUid, string email, string? displayName, CancellationToken ct = default);
    Task<UserProfileDto> GetProfileAsync(string firebaseUid, CancellationToken ct = default);
}
