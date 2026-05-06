using Microsoft.Extensions.Options;
using StreakPlatform.Application.Common;
using StreakPlatform.Application.DTOs;
using StreakPlatform.Application.Interfaces;
using StreakPlatform.Domain.Entities;

namespace StreakPlatform.Application.Services;

public class CheckInService : ICheckInService
{
    private readonly IUserRepository _users;
    private readonly IStreakRepository _streaks;
    private readonly IParticipantRepository _participants;
    private readonly ICheckInRepository _checkIns;
    private readonly IProtectionRepository _protections;
    private readonly IPointsService _points;
    private readonly IUnitOfWork _uow;
    private readonly AppOptions _options;

    public CheckInService(
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

    public async Task<CheckInResultDto> RecordAsync(string firebaseUid, Guid streakId, CheckInRequest req, CancellationToken ct = default)
    {
        var user = await _users.GetByFirebaseUidAsync(firebaseUid, ct)
            ?? throw new NotFoundException("User not initialized.");

        var streak = await _streaks.GetByIdAsync(streakId, ct)
            ?? throw new NotFoundException("Streak not found.");

        if (!await _participants.ExistsAsync(user.Id, streakId, ct))
            throw new ForbiddenException("You are not a participant of this streak.");

        var note = string.IsNullOrWhiteSpace(req.Note) ? null : req.Note.Trim();
        var mediaUrl = string.IsNullOrWhiteSpace(req.MediaUrl) ? null : req.MediaUrl.Trim();
        var mediaContentType = string.IsNullOrWhiteSpace(req.MediaContentType) ? null : req.MediaContentType.Trim();

        if (streak.RequiresProof && note is null && mediaUrl is null)
            throw new ValidationException("This streak requires a note or photo as proof.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var yesterday = today.AddDays(-1);
        var dayBefore = today.AddDays(-2);

        if (await _checkIns.ExistsAsync(user.Id, streakId, today, ct))
            throw new ConflictException("Already checked in today.");

        var existingCheckIns = await _checkIns.GetUserDatesAsync(user.Id, streakId, ct);
        var existingProtected = await _protections.GetUsedDatesAsync(user.Id, streakId, ct);
        var combined = new HashSet<DateOnly>(existingCheckIns);
        combined.UnionWith(existingProtected);

        var gapIsYesterday = !combined.Contains(yesterday) && combined.Contains(dayBefore);
        var protectionConsumed = false;

        if (gapIsYesterday)
        {
            var pending = await _protections.GetPendingAsync(user.Id, streakId, ct);
            if (pending is not null && user.PointsBalance >= _options.ProtectionCost)
            {
                pending.Status = ProtectionStatus.Used;
                pending.AppliedToDate = yesterday;
                pending.AppliedAt = DateTime.UtcNow;

                await _points.AwardAsync(user.Id, -_options.ProtectionCost,
                    PointsTransactionReason.ProtectionPurchase, streakId, pending.Id, ct);

                existingProtected = existingProtected.Append(yesterday).ToList();
                protectionConsumed = true;
            }
        }

        await _checkIns.AddAsync(new CheckIn
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            StreakId = streakId,
            Date = today,
            CreatedAt = DateTime.UtcNow,
            Note = note,
            MediaUrl = mediaUrl,
            MediaContentType = mediaContentType
        }, ct);

        var newBalance = await _points.AwardAsync(user.Id, _options.PointsPerCheckIn,
            PointsTransactionReason.CheckInReward, streakId, null, ct);

        await _uow.SaveChangesAsync(ct);

        var dates = existingCheckIns.Append(today);
        var count = StreakCountCalculator.Compute(dates, existingProtected, today);
        return new CheckInResultDto(streakId, today, count, _options.PointsPerCheckIn, newBalance, protectionConsumed);
    }

    public async Task<TodayStatusDto> GetTodayStatusAsync(string firebaseUid, Guid streakId, CancellationToken ct = default)
    {
        var user = await _users.GetByFirebaseUidAsync(firebaseUid, ct)
            ?? throw new NotFoundException("User not initialized.");

        if (!await _participants.ExistsAsync(user.Id, streakId, ct))
            throw new ForbiddenException("You are not a participant of this streak.");

        var streak = await _streaks.GetByIdWithParticipantsAsync(streakId, ct)
            ?? throw new NotFoundException("Streak not found.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var checkedInUserIds = (await _checkIns.GetUsersCheckedInOnDateAsync(streakId, today, ct)).ToHashSet();

        var roster = streak.Participants
            .OrderBy(p => p.JoinedAt)
            .Select(p => new TodayParticipantDto(
                p.UserId,
                p.User.DisplayName ?? p.User.Email,
                checkedInUserIds.Contains(p.UserId)))
            .ToList();

        return new TodayStatusDto(streakId, today, roster);
    }

    public async Task<StreakStatusDto> GetStreakStatusAsync(string firebaseUid, Guid streakId, CancellationToken ct = default)
    {
        var user = await _users.GetByFirebaseUidAsync(firebaseUid, ct)
            ?? throw new NotFoundException("User not initialized.");

        if (!await _participants.ExistsAsync(user.Id, streakId, ct))
            throw new ForbiddenException("You are not a participant of this streak.");

        var streak = await _streaks.GetByIdWithParticipantsAsync(streakId, ct)
            ?? throw new NotFoundException("Streak not found.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var userIds = streak.Participants.Select(p => p.UserId).ToList();
        var datesByUser = await _checkIns.GetDatesByUsersAsync(userIds, streakId, ct);
        var protectedByUser = await _protections.GetUsedDatesByUsersAsync(userIds, streakId, ct);

        var participants = streak.Participants
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

        return new StreakStatusDto(streakId, today, participants);
    }

    public async Task<IReadOnlyList<CheckInFeedItemDto>> GetFeedAsync(string firebaseUid, Guid streakId, int take, int skip, CancellationToken ct = default)
    {
        var user = await _users.GetByFirebaseUidAsync(firebaseUid, ct)
            ?? throw new NotFoundException("User not initialized.");

        if (!await _participants.ExistsAsync(user.Id, streakId, ct))
            throw new ForbiddenException("You are not a participant of this streak.");

        var rows = await _checkIns.GetFeedAsync(streakId, Math.Clamp(take, 1, 100), Math.Max(skip, 0), ct);
        return rows.Select(r => new CheckInFeedItemDto(
            r.CheckIn.Id,
            r.CheckIn.UserId,
            r.DisplayName,
            r.CheckIn.Date,
            r.CheckIn.CreatedAt,
            r.CheckIn.Note,
            r.CheckIn.MediaUrl,
            r.CheckIn.MediaContentType)).ToList();
    }
}
