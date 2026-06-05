using Microsoft.Extensions.Options;
using StreakPlatform.Application.Common;
using StreakPlatform.Application.DTOs;
using StreakPlatform.Application.Interfaces;
using StreakPlatform.Domain.Entities;

namespace StreakPlatform.Application.Services;

public class MissedDayRecoveryService : IMissedDayRecoveryService
{
    private readonly IUserRepository _users;
    private readonly IStreakRepository _streaks;
    private readonly IParticipantRepository _participants;
    private readonly ICheckInRepository _checkIns;
    private readonly IProtectionRepository _protections;
    private readonly IPointsService _points;
    private readonly IUnitOfWork _uow;
    private readonly AppOptions _options;

    public MissedDayRecoveryService(
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

    public async Task<DebtDto> GetDebtAsync(string firebaseUid, Guid streakId, CancellationToken ct = default)
    {
        var (user, participant) = await LoadActiveOrThrow(firebaseUid, streakId, ct);
        var missed = await ComputeMissedDatesAsync(user.Id, streakId, participant.JoinedAt, ct);
        var unit = _options.MissedDayRecoveryCost;
        var total = unit * missed.Count;
        return new DebtDto(streakId, missed.Count, unit, total, user.PointsBalance, user.PointsBalance >= total, missed);
    }

    public async Task<PayDebtResultDto> PayDebtAsync(string firebaseUid, Guid streakId, CancellationToken ct = default)
    {
        var (user, participant) = await LoadActiveOrThrow(firebaseUid, streakId, ct);
        var missed = await ComputeMissedDatesAsync(user.Id, streakId, participant.JoinedAt, ct);

        if (missed.Count == 0)
            return new PayDebtResultDto(streakId, 0, 0, user.PointsBalance, Array.Empty<DateOnly>());

        var unit = _options.MissedDayRecoveryCost;
        var total = unit * missed.Count;
        if (user.PointsBalance < total)
            throw new ConflictException($"insufficient_points: Need {total} pts (you have {user.PointsBalance}).");

        // Create one StreakProtection row per missed day (Status=Used) — these flow into the dynamic count calculator.
        var now = DateTime.UtcNow;
        foreach (var d in missed)
        {
            await _protections.AddAsync(new StreakProtection
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                StreakId = streakId,
                Status = ProtectionStatus.Used,
                PointsCost = unit,
                ScheduledAt = now,
                AppliedToDate = d,
                AppliedAt = now,
                CreatedAt = now
            }, ct);
        }

        var newBalance = await _points.AwardAsync(user.Id, -total,
            PointsTransactionReason.MissedDayRecovery, streakId, null, ct);

        await _uow.SaveChangesAsync(ct);
        return new PayDebtResultDto(streakId, missed.Count, total, newBalance, missed);
    }

    public async Task<(int NewBalance, bool NeedsToBuyPoints)> DeductOnLoginAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException("User not found.");

        var activeParticipants = await _participants.GetActiveByUserIdAsync(userId, ct);
        if (activeParticipants.Count == 0)
            return (user.PointsBalance, user.PointsBalance <= 0);

        var now = DateTime.UtcNow;
        var allMissed = new List<(Guid StreakId, DateOnly Date)>();

        foreach (var p in activeParticipants)
        {
            var missed = await ComputeMissedDatesAsync(userId, p.StreakId, p.JoinedAt, ct);
            foreach (var d in missed)
                allMissed.Add((p.StreakId, d));
        }

        if (allMissed.Count == 0)
            return (user.PointsBalance, user.PointsBalance <= 0);

        var unit = _options.MissedDayRecoveryCost;
        var totalDebt = unit * allMissed.Count;
        var insufficient = user.PointsBalance < totalDebt;

        // Create one StreakProtection row per missed day (marks them as settled for streak-count purposes)
        foreach (var (streakId, date) in allMissed)
        {
            await _protections.AddAsync(new StreakProtection
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                StreakId = streakId,
                Status = ProtectionStatus.Used,
                PointsCost = unit,
                ScheduledAt = now,
                AppliedToDate = date,
                AppliedAt = now,
                CreatedAt = now
            }, ct);
        }

        // AwardAsync clamps balance at 0 — deducts what is available
        var newBalance = await _points.AwardAsync(userId, -totalDebt,
            PointsTransactionReason.MissedDayRecovery, null, null, ct);

        await _uow.SaveChangesAsync(ct);
        return (newBalance, insufficient || newBalance <= 0);
    }

    public async Task LeaveAsync(string firebaseUid, Guid streakId, CancellationToken ct = default)
    {
        var user = await _users.GetByFirebaseUidAsync(firebaseUid, ct)
            ?? throw new NotFoundException("User not initialized.");

        var participant = await _participants.GetAsync(user.Id, streakId, ct);
        if (participant is null || !participant.IsActive)
            return; // idempotent

        participant.IsActive = false;
        participant.InactiveAt = DateTime.UtcNow;
        participant.InactiveReason = "user_left";
        await _uow.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Returns calendar days the user MISSED — days strictly between (last activity OR participant join) and today,
    /// excluding any days already covered by check-in or prior recovery.
    /// </summary>
    private async Task<IReadOnlyList<DateOnly>> ComputeMissedDatesAsync(Guid userId, Guid streakId, DateTime joinedAt, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var yesterday = today.AddDays(-1);

        var checkInDates = await _checkIns.GetUserDatesAsync(userId, streakId, ct);
        var protectedDates = await _protections.GetUsedDatesAsync(userId, streakId, ct);

        var covered = new HashSet<DateOnly>(checkInDates);
        covered.UnionWith(protectedDates);

        // Anchor = max(joinedDate, mostRecentActivity). We start walking AFTER the anchor, ending at yesterday.
        var joinedDate = DateOnly.FromDateTime(joinedAt);
        var lastCovered = covered.Count == 0 ? joinedDate : covered.Max();
        var anchor = joinedDate > lastCovered ? joinedDate : lastCovered;

        if (anchor >= yesterday) return Array.Empty<DateOnly>();

        var missed = new List<DateOnly>();
        for (var d = anchor.AddDays(1); d <= yesterday; d = d.AddDays(1))
        {
            if (!covered.Contains(d)) missed.Add(d);
        }
        return missed;
    }

    private async Task<(User user, Participant participant)> LoadActiveOrThrow(string firebaseUid, Guid streakId, CancellationToken ct)
    {
        var user = await _users.GetByFirebaseUidAsync(firebaseUid, ct)
            ?? throw new NotFoundException("User not initialized.");

        if (await _streaks.GetByIdAsync(streakId, ct) is null)
            throw new NotFoundException("Streak not found.");

        var participant = await _participants.GetAsync(user.Id, streakId, ct);
        if (participant is null || !participant.IsActive)
            throw new ForbiddenException("You are not a participant of this streak.");

        return (user, participant);
    }
}
