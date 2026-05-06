using Microsoft.Extensions.Options;
using StreakPlatform.Application.Common;
using StreakPlatform.Application.DTOs;
using StreakPlatform.Application.Interfaces;
using StreakPlatform.Domain.Entities;

namespace StreakPlatform.Application.Services;

public class StreakProtectionService : IStreakProtectionService
{
    private readonly IUserRepository _users;
    private readonly IStreakRepository _streaks;
    private readonly IParticipantRepository _participants;
    private readonly ICheckInRepository _checkIns;
    private readonly IProtectionRepository _protections;
    private readonly IPointsService _points;
    private readonly IUnitOfWork _uow;
    private readonly AppOptions _options;

    public StreakProtectionService(
        IUserRepository users,
        IStreakRepository streaks,
        IParticipantRepository participants,
        ICheckInRepository checkIns,
        IProtectionRepository protections,
        IPointsService points,
        IUnitOfWork uow,
        IOptions<AppOptions> options)
    {
        _users = users;
        _streaks = streaks;
        _participants = participants;
        _checkIns = checkIns;
        _protections = protections;
        _points = points;
        _uow = uow;
        _options = options.Value;
    }

    public async Task<ProtectionDto> ActivateAsync(string firebaseUid, Guid streakId, CancellationToken ct = default)
    {
        var user = await GetUserOrThrow(firebaseUid, ct);
        await EnsureParticipantOrThrow(user.Id, streakId, ct);

        // Idempotent: if a Pending one already exists, return it.
        var existing = await _protections.GetPendingAsync(user.Id, streakId, ct);
        if (existing is not null)
            return Map(existing);

        if (user.PointsBalance < _options.ProtectionCost)
            throw new ConflictException($"Insufficient points. You need {_options.ProtectionCost} to protect this streak.");

        var now = DateTime.UtcNow;
        var protection = new StreakProtection
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            StreakId = streakId,
            Status = ProtectionStatus.Pending,
            PointsCost = _options.ProtectionCost,
            ScheduledAt = now,
            CreatedAt = now
        };
        await _protections.AddAsync(protection, ct);
        await _uow.SaveChangesAsync(ct);
        return Map(protection);
    }

    public async Task CancelAsync(string firebaseUid, Guid streakId, CancellationToken ct = default)
    {
        var user = await GetUserOrThrow(firebaseUid, ct);
        var existing = await _protections.GetPendingAsync(user.Id, streakId, ct);
        if (existing is null)
            return;

        existing.Status = ProtectionStatus.Cancelled;
        await _uow.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ProtectionDto>> GetMyPendingAsync(string firebaseUid, CancellationToken ct = default)
    {
        var user = await GetUserOrThrow(firebaseUid, ct);
        var pending = await _protections.GetPendingForUserAsync(user.Id, ct);
        return pending.Select(Map).ToList();
    }

    public async Task<RestoreResultDto> RestoreAsync(string firebaseUid, Guid streakId, CancellationToken ct = default)
    {
        var user = await GetUserOrThrow(firebaseUid, ct);
        await EnsureParticipantOrThrow(user.Id, streakId, ct);

        if (user.PointsBalance < _options.ProtectionCost)
            throw new ConflictException("Insufficient points.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var yesterday = today.AddDays(-1);

        var checkInDates = await _checkIns.GetUserDatesAsync(user.Id, streakId, ct);
        var protectedDates = await _protections.GetUsedDatesAsync(user.Id, streakId, ct);
        var combined = new HashSet<DateOnly>(checkInDates);
        combined.UnionWith(protectedDates);

        // Eligibility: yesterday must NOT be in chain, day-before-yesterday MUST be in chain.
        if (combined.Contains(yesterday))
            throw new ConflictException("Nothing to restore — yesterday is already in your streak.");
        if (!combined.Contains(today.AddDays(-2)))
            throw new ConflictException("Nothing to restore — your streak is broken beyond yesterday.");

        // Within 24h window — entire calendar today is the window.
        // (Window expires at end-of-today UTC.)

        var now = DateTime.UtcNow;
        var protection = new StreakProtection
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            StreakId = streakId,
            Status = ProtectionStatus.Used,
            PointsCost = _options.ProtectionCost,
            ScheduledAt = now,
            AppliedToDate = yesterday,
            AppliedAt = now,
            CreatedAt = now
        };
        await _protections.AddAsync(protection, ct);

        var newBalance = await _points.AwardAsync(user.Id, -_options.ProtectionCost,
            PointsTransactionReason.Restore, streakId, protection.Id, ct);

        await _uow.SaveChangesAsync(ct);

        // Compute restored count (yesterday is now in the chain via protection).
        combined.Add(yesterday);
        var newCount = StreakCountCalculator.Compute(checkInDates, protectedDates.Append(yesterday), today);

        return new RestoreResultDto(streakId, yesterday, newBalance, newCount);
    }

    private async Task<User> GetUserOrThrow(string firebaseUid, CancellationToken ct) =>
        await _users.GetByFirebaseUidAsync(firebaseUid, ct)
            ?? throw new NotFoundException("User not initialized.");

    private async Task EnsureParticipantOrThrow(Guid userId, Guid streakId, CancellationToken ct)
    {
        if (!await _participants.ExistsAsync(userId, streakId, ct))
            throw new ForbiddenException("You are not a participant of this streak.");
    }

    private static ProtectionDto Map(StreakProtection p) =>
        new(p.Id, p.StreakId, p.Status.ToString(), p.PointsCost, p.ScheduledAt, p.AppliedToDate);
}
