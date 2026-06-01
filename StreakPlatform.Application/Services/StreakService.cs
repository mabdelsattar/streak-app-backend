using Microsoft.Extensions.Options;
using StreakPlatform.Application.Common;
using StreakPlatform.Application.DTOs;
using StreakPlatform.Application.Interfaces;
using StreakPlatform.Domain.Entities;

namespace StreakPlatform.Application.Services;

public class StreakService : IStreakService
{
    private readonly IUserRepository _users;
    private readonly IStreakRepository _streaks;
    private readonly IParticipantRepository _participants;
    private readonly ICheckInRepository _checkIns;
    private readonly IProtectionRepository _protections;
    private readonly IPointsService _points;
    private readonly IUnitOfWork _uow;
    private readonly InviteCodeGenerator _codes;
    private readonly InviteUrlBuilder _urlBuilder;
    private readonly AppOptions _options;

    public StreakService(
        IUserRepository users,
        IStreakRepository streaks,
        IParticipantRepository participants,
        ICheckInRepository checkIns,
        IProtectionRepository protections,
        IPointsService points,
        IUnitOfWork uow,
        InviteCodeGenerator codes,
        InviteUrlBuilder urlBuilder,
        IOptions<AppOptions> options)
    {
        _users = users;
        _streaks = streaks;
        _participants = participants;
        _checkIns = checkIns;
        _protections = protections;
        _points = points;
        _uow = uow;
        _codes = codes;
        _urlBuilder = urlBuilder;
        _options = options.Value;
    }

    public async Task<StreakDetailDto> CreateAsync(string firebaseUid, CreateStreakRequest req, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            throw new ValidationException("Streak name is required.");

        var user = await GetUserOrThrow(firebaseUid, ct);
        var now = DateTime.UtcNow;
        var label = string.IsNullOrWhiteSpace(req.CheckInButtonLabel) ? null : req.CheckInButtonLabel.Trim();

        var streak = new Streak
        {
            Id = Guid.NewGuid(),
            Name = req.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim(),
            CreatedBy = user.Id,
            InviteCode = await _codes.GenerateUniqueAsync(ct),
            CheckInType = req.CheckInType,
            CheckInButtonLabel = req.CheckInType == CheckInType.Action ? (label ?? "Done") : label,
            IsPublic = req.IsPublic,
            CreatedAt = now
        };
        await _streaks.AddAsync(streak, ct);

        await _participants.AddAsync(new Participant
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            StreakId = streak.Id,
            JoinedAt = now,
            IsActive = true
        }, ct);

        await _uow.SaveChangesAsync(ct);
        return await BuildDetailAsync(user.Id, streak.Id, ct);
    }

    public async Task<IReadOnlyList<StreakSummaryDto>> GetMyStreaksAsync(string firebaseUid, CancellationToken ct = default)
    {
        var user = await GetUserOrThrow(firebaseUid, ct);
        var streaks = await _streaks.GetForUserAsync(user.Id, ct);
        if (streaks.Count == 0) return Array.Empty<StreakSummaryDto>();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var summaries = new List<StreakSummaryDto>(streaks.Count);

        foreach (var s in streaks)
        {
            var dates = await _checkIns.GetUserDatesAsync(user.Id, s.Id, ct);
            var prots = await _protections.GetUsedDatesAsync(user.Id, s.Id, ct);

            summaries.Add(new StreakSummaryDto(
                s.Id,
                s.Name,
                s.Description,
                StreakCountCalculator.Compute(dates, prots, today),
                StreakCountCalculator.CheckedInToday(dates, today),
                s.Participants.Count(p => p.IsActive),
                s.CheckInType.ToString(),
                s.CheckInButtonLabel,
                s.IsPublic));
        }
        return summaries;
    }

    public async Task<StreakDetailDto> GetDetailAsync(string firebaseUid, Guid streakId, CancellationToken ct = default)
    {
        var user = await GetUserOrThrow(firebaseUid, ct);
        await EnsureActiveParticipantOrThrow(user.Id, streakId, ct);
        return await BuildDetailAsync(user.Id, streakId, ct);
    }

    public async Task<StreakDetailDto> JoinByInviteCodeAsync(string firebaseUid, string inviteCode, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(inviteCode))
            throw new ValidationException("Invite code is required.");

        var user = await GetUserOrThrow(firebaseUid, ct);
        var streak = await _streaks.GetByInviteCodeAsync(inviteCode.Trim().ToUpperInvariant(), ct)
            ?? throw new NotFoundException("Invalid invite code.");

        await UpsertActiveParticipantAsync(user.Id, streak.Id, ct);
        await _uow.SaveChangesAsync(ct);

        return await BuildDetailAsync(user.Id, streak.Id, ct);
    }

    public async Task<InviteDto> GetInviteAsync(string firebaseUid, Guid streakId, CancellationToken ct = default)
    {
        var user = await GetUserOrThrow(firebaseUid, ct);
        await EnsureActiveParticipantOrThrow(user.Id, streakId, ct);
        var streak = await _streaks.GetByIdAsync(streakId, ct)
            ?? throw new NotFoundException("Streak not found.");
        return new InviteDto(streak.Id, streak.InviteCode, _urlBuilder.Build(streak.InviteCode));
    }

    public async Task<IReadOnlyList<PublicStreakDto>> GetPublicStreaksAsync(
        string firebaseUid, int take, int skip, string? search, CancellationToken ct = default)
    {
        var user = await GetUserOrThrow(firebaseUid, ct);
        var effectiveTake = take <= 0 ? _options.PublicListDefaultTake : take;
        var streaks = await _streaks.GetPublicForDiscoveryAsync(user.Id, effectiveTake, skip, search, ct);
        return streaks.Select(s => new PublicStreakDto(
            s.Id,
            s.Name,
            s.Description,
            s.Participants.Count(p => p.IsActive),
            s.CheckInType.ToString(),
            s.CreatedAt,
            s.Creator?.DisplayName ?? s.Creator?.Email ?? "—")).ToList();
    }

    public async Task<StreakDetailDto> JoinPublicAsync(string firebaseUid, Guid streakId, CancellationToken ct = default)
    {
        var user = await GetUserOrThrow(firebaseUid, ct);

        var streak = await _streaks.GetByIdAsync(streakId, ct)
            ?? throw new NotFoundException("Streak not found.");

        if (!streak.IsPublic)
            throw new ForbiddenException("This streak is private. Use the invite code to join.");

        var cost = _options.PublicStreakJoinCost;
        if (cost > 0)
        {
            if (user.PointsBalance < cost)
                throw new ConflictException($"insufficient_points: Need {cost} pts to join.");
            await _points.AwardAsync(user.Id, -cost, PointsTransactionReason.ProtectionPurchase, streakId, null, ct);
        }

        await UpsertActiveParticipantAsync(user.Id, streak.Id, ct);
        await _uow.SaveChangesAsync(ct);

        return await BuildDetailAsync(user.Id, streak.Id, ct);
    }

    /// <summary>
    /// Activates a Participant row for (user, streak). If one already exists:
    /// - active → throw ConflictException("already_joined")
    /// - inactive → reactivate and reset JoinedAt to now (fresh start)
    /// </summary>
    private async Task UpsertActiveParticipantAsync(Guid userId, Guid streakId, CancellationToken ct)
    {
        var existing = await _participants.GetAsync(userId, streakId, ct);
        if (existing is not null)
        {
            if (existing.IsActive)
                throw new ConflictException("You have already joined this streak.");
            existing.IsActive = true;
            existing.InactiveAt = null;
            existing.InactiveReason = null;
            existing.JoinedAt = DateTime.UtcNow;   // fresh start — streak count begins again
        }
        else
        {
            await _participants.AddAsync(new Participant
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                StreakId = streakId,
                JoinedAt = DateTime.UtcNow,
                IsActive = true
            }, ct);
        }
    }

    private async Task<User> GetUserOrThrow(string firebaseUid, CancellationToken ct) =>
        await _users.GetByFirebaseUidAsync(firebaseUid, ct)
            ?? throw new NotFoundException("User not initialized.");

    private async Task EnsureActiveParticipantOrThrow(Guid userId, Guid streakId, CancellationToken ct)
    {
        if (!await _participants.IsActiveAsync(userId, streakId, ct))
            throw new ForbiddenException("You are not a participant of this streak.");
    }

    private async Task<StreakDetailDto> BuildDetailAsync(Guid currentUserId, Guid streakId, CancellationToken ct)
    {
        var streak = await _streaks.GetByIdWithParticipantsAsync(streakId, ct)
            ?? throw new NotFoundException("Streak not found.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var active = streak.Participants.Where(p => p.IsActive).ToList();
        var userIds = active.Select(p => p.UserId).ToList();
        var datesByUser = await _checkIns.GetDatesByUsersAsync(userIds, streak.Id, ct);
        var protectedByUser = await _protections.GetUsedDatesByUsersAsync(userIds, streak.Id, ct);

        var participants = active
            .OrderBy(p => p.JoinedAt)
            .Select(p =>
            {
                var dates = datesByUser.TryGetValue(p.UserId, out var d) ? d : Array.Empty<DateOnly>();
                var prots = protectedByUser.TryGetValue(p.UserId, out var pp) ? pp : Array.Empty<DateOnly>();
                return new ParticipantStatusDto(
                    p.UserId,
                    p.User.DisplayName ?? p.User.Email,
                    StreakCountCalculator.Compute(dates, prots, today),
                    StreakCountCalculator.CheckedInToday(dates, today),
                    p.JoinedAt);
            })
            .ToList();

        var me = participants.First(x => x.UserId == currentUserId);

        // Compute debt for the current user
        var myParticipant = active.First(p => p.UserId == currentUserId);
        var myDates = datesByUser.TryGetValue(currentUserId, out var md) ? md : Array.Empty<DateOnly>();
        var myProts = protectedByUser.TryGetValue(currentUserId, out var mp) ? mp : Array.Empty<DateOnly>();
        var missedDays = ComputeMissedDayCount(myDates, myProts, myParticipant.JoinedAt, today);
        var unit = _options.MissedDayRecoveryCost;
        var totalCost = unit * missedDays;

        var me2 = await _users.GetByIdAsync(currentUserId, ct);
        var balance = me2?.PointsBalance ?? 0;

        return new StreakDetailDto(
            streak.Id,
            streak.Name,
            streak.Description,
            streak.InviteCode,
            _urlBuilder.Build(streak.InviteCode),
            streak.CreatedAt,
            streak.CreatedBy,
            me.CurrentCount,
            me.CheckedInToday,
            balance,
            missedDays,
            unit,
            totalCost,
            balance >= totalCost,
            streak.CheckInType.ToString(),
            streak.CheckInButtonLabel,
            streak.IsPublic,
            participants);
    }

    private static int ComputeMissedDayCount(IReadOnlyList<DateOnly> checkIns, IReadOnlyList<DateOnly> protectedDates, DateTime joinedAt, DateOnly today)
    {
        var yesterday = today.AddDays(-1);
        var joinedDate = DateOnly.FromDateTime(joinedAt);
        var covered = new HashSet<DateOnly>(checkIns);
        covered.UnionWith(protectedDates);
        var lastCovered = covered.Count == 0 ? joinedDate : covered.Max();
        var anchor = joinedDate > lastCovered ? joinedDate : lastCovered;
        if (anchor >= yesterday) return 0;

        var count = 0;
        for (var d = anchor.AddDays(1); d <= yesterday; d = d.AddDays(1))
            if (!covered.Contains(d)) count++;
        return count;
    }
}
