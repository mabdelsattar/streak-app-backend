using StreakPlatform.Application.Common;
using StreakPlatform.Application.DTOs;
using StreakPlatform.Application.Interfaces;
using StreakPlatform.Domain.Entities;

namespace StreakPlatform.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _users;
    private readonly IUnitOfWork _uow;

    public UserService(IUserRepository users, IUnitOfWork uow)
    {
        _users = users;
        _uow = uow;
    }

    public async Task<UserProfileDto> InitializeAsync(string firebaseUid, string email, string? displayName, CancellationToken ct = default)
    {
        var existing = await _users.GetByFirebaseUidAsync(firebaseUid, ct);
        if (existing is not null)
        {
            // Optionally update display name on subsequent inits.
            if (!string.IsNullOrWhiteSpace(displayName) && existing.DisplayName != displayName)
            {
                existing.DisplayName = displayName;
                existing.UpdatedAt = DateTime.UtcNow;
                await _uow.SaveChangesAsync(ct);
            }
            return Map(existing);
        }

        var now = DateTime.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirebaseUid = firebaseUid,
            Email = email,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? FallbackDisplayName(email) : displayName,
            CreatedAt = now,
            UpdatedAt = now
        };
        await _users.AddAsync(user, ct);
        await _uow.SaveChangesAsync(ct);
        return Map(user);
    }

    public async Task<UserProfileDto> GetProfileAsync(string firebaseUid, CancellationToken ct = default)
    {
        var user = await _users.GetByFirebaseUidAsync(firebaseUid, ct)
            ?? throw new NotFoundException("User not initialized. Call /api/auth/initialize first.");
        return Map(user);
    }

    private static string FallbackDisplayName(string email)
    {
        var at = email.IndexOf('@');
        return at > 0 ? email[..at] : email;
    }

    private static UserProfileDto Map(User u) => new(u.Id, u.Email, u.DisplayName);
}
