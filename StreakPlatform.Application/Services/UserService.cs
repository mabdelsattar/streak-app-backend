using Microsoft.Extensions.Options;
using StreakPlatform.Application.Common;
using StreakPlatform.Application.DTOs;
using StreakPlatform.Application.Interfaces;
using StreakPlatform.Domain.Entities;

namespace StreakPlatform.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _users;
    private readonly IPointsTransactionRepository _txs;
    private readonly IUnitOfWork _uow;
    private readonly AppOptions _options;

    public UserService(
        IUserRepository users,
        IPointsTransactionRepository txs,
        IUnitOfWork uow,
        IOptions<AppOptions> options)
    {
        _users = users;
        _txs = txs;
        _uow = uow;
        _options = options.Value;
    }

    public async Task<UserProfileDto> InitializeAsync(string firebaseUid, string email, string? displayName, CancellationToken ct = default)
    {
        var existing = await _users.GetByFirebaseUidAsync(firebaseUid, ct);
        if (existing is not null)
        {
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
            PointsBalance = _options.StartingPointsBalance,
            CreatedAt = now,
            UpdatedAt = now
        };
        await _users.AddAsync(user, ct);

        // Record the starting grant for audit.
        if (_options.StartingPointsBalance > 0)
        {
            await _txs.AddAsync(new PointsTransaction
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Delta = _options.StartingPointsBalance,
                Reason = PointsTransactionReason.StartingGrant,
                CreatedAt = now
            }, ct);
        }

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
