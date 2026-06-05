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
    private readonly IReactionRepository _reactions;
    private readonly IPointsService _points;
    private readonly IUnitOfWork _uow;
    private readonly AppOptions _options;

    public CheckInService(
        IUserRepository users,
        IStreakRepository streaks,
        IParticipantRepository participants,
        ICheckInRepository checkIns,
        IProtectionRepository protections,
        IReactionRepository reactions,
        IPointsService points,
        IUnitOfWork uow,
        IOptions<AppOptions> options)
    {
        _users = users;
        _streaks = streaks;
        _participants = participants;
        _checkIns = checkIns;
        _protections = protections;
        _reactions = reactions;
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

        var participant = await _participants.GetAsync(user.Id, streakId, ct);
        if (participant is null || !participant.IsActive)
            throw new ForbiddenException("You are not a participant of this streak.");

        var note = string.IsNullOrWhiteSpace(req.Note) ? null : req.Note.Trim();
        var mediaUrl = string.IsNullOrWhiteSpace(req.MediaUrl) ? null : req.MediaUrl.Trim();
        var mediaContentType = string.IsNullOrWhiteSpace(req.MediaContentType) ? null : req.MediaContentType.Trim();
        var duration = req.MediaDurationSeconds;

        ValidateForType(streak.CheckInType, note, mediaUrl, mediaContentType);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (await _checkIns.ExistsAsync(user.Id, streakId, today, ct))
            throw new ConflictException("Already checked in today.");

        // Debt is auto-settled on login. If balance still reached 0 the user must top up before continuing.
        if (user.PointsBalance <= 0)
            throw new ConflictException("insufficient_points: Your balance is 0. Buy points to continue participating.");

        var existingCheckIns = await _checkIns.GetUserDatesAsync(user.Id, streakId, ct);
        var existingProtected = await _protections.GetUsedDatesAsync(user.Id, streakId, ct);

        await _checkIns.AddAsync(new CheckIn
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            StreakId = streakId,
            Date = today,
            CreatedAt = DateTime.UtcNow,
            Note = note,
            MediaUrl = mediaUrl,
            MediaContentType = mediaContentType,
            MediaDurationSeconds = duration
        }, ct);

        var newBalance = await _points.AwardAsync(user.Id, _options.PointsPerCheckIn,
            PointsTransactionReason.CheckInReward, streakId, null, ct);

        await _uow.SaveChangesAsync(ct);

        var dates = existingCheckIns.Append(today);
        var count = StreakCountCalculator.Compute(dates, existingProtected, today);
        return new CheckInResultDto(streakId, today, count, _options.PointsPerCheckIn, newBalance, false);
    }

    public async Task<TodayStatusDto> GetTodayStatusAsync(string firebaseUid, Guid streakId, CancellationToken ct = default)
    {
        var user = await _users.GetByFirebaseUidAsync(firebaseUid, ct)
            ?? throw new NotFoundException("User not initialized.");

        if (!await _participants.IsActiveAsync(user.Id, streakId, ct))
            throw new ForbiddenException("You are not a participant of this streak.");

        var streak = await _streaks.GetByIdWithParticipantsAsync(streakId, ct)
            ?? throw new NotFoundException("Streak not found.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var checkedInUserIds = (await _checkIns.GetUsersCheckedInOnDateAsync(streakId, today, ct)).ToHashSet();

        var roster = streak.Participants
            .Where(p => p.IsActive)
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

        if (!await _participants.IsActiveAsync(user.Id, streakId, ct))
            throw new ForbiddenException("You are not a participant of this streak.");

        var streak = await _streaks.GetByIdWithParticipantsAsync(streakId, ct)
            ?? throw new NotFoundException("Streak not found.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var active = streak.Participants.Where(p => p.IsActive).ToList();
        var userIds = active.Select(p => p.UserId).ToList();
        var datesByUser = await _checkIns.GetDatesByUsersAsync(userIds, streakId, ct);
        var protectedByUser = await _protections.GetUsedDatesByUsersAsync(userIds, streakId, ct);

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

        return new StreakStatusDto(streakId, today, participants);
    }

    public async Task<IReadOnlyList<CheckInFeedItemDto>> GetFeedAsync(string firebaseUid, Guid streakId, int take, int skip, CancellationToken ct = default)
    {
        var user = await _users.GetByFirebaseUidAsync(firebaseUid, ct)
            ?? throw new NotFoundException("User not initialized.");

        if (!await _participants.IsActiveAsync(user.Id, streakId, ct))
            throw new ForbiddenException("You are not a participant of this streak.");

        var rows = await _checkIns.GetFeedAsync(streakId, Math.Clamp(take, 1, 100), Math.Max(skip, 0), ct);
        if (rows.Count == 0) return Array.Empty<CheckInFeedItemDto>();

        var ids = rows.Select(r => r.CheckIn.Id).ToList();
        var counts = await _reactions.CountByCheckInAsync(ids, ct);
        var mine = await _reactions.GetMyReactionsByCheckInAsync(ids, user.Id, ct);

        return rows.Select(r =>
        {
            var (likes, dislikes) = counts.TryGetValue(r.CheckIn.Id, out var c) ? c : (0, 0);
            string? myReaction = mine.TryGetValue(r.CheckIn.Id, out var t) ? t.ToString() : null;
            return new CheckInFeedItemDto(
                r.CheckIn.Id,
                r.CheckIn.UserId,
                r.DisplayName,
                r.CheckIn.Date,
                r.CheckIn.CreatedAt,
                r.CheckIn.Note,
                r.CheckIn.MediaUrl,
                r.CheckIn.MediaContentType,
                r.CheckIn.MediaDurationSeconds,
                likes,
                dislikes,
                myReaction,
                r.CheckIn.UserId == user.Id);
        }).ToList();
    }

    private static void ValidateForType(CheckInType type, string? note, string? mediaUrl, string? mediaContentType)
    {
        switch (type)
        {
            case CheckInType.Action:
                return;
            case CheckInType.Text:
                if (string.IsNullOrWhiteSpace(note))
                    throw new ValidationException("This streak requires a text note.");
                return;
            case CheckInType.Image:
                if (string.IsNullOrWhiteSpace(mediaUrl) || mediaContentType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) != true)
                    throw new ValidationException("This streak requires an image.");
                return;
            case CheckInType.Voice:
                if (string.IsNullOrWhiteSpace(mediaUrl) || mediaContentType?.StartsWith("audio/", StringComparison.OrdinalIgnoreCase) != true)
                    throw new ValidationException("This streak requires a voice recording.");
                return;
            case CheckInType.Video:
                if (string.IsNullOrWhiteSpace(mediaUrl) || mediaContentType?.StartsWith("video/", StringComparison.OrdinalIgnoreCase) != true)
                    throw new ValidationException("This streak requires a video recording.");
                return;
        }
    }
}
