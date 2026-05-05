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
    private readonly IUnitOfWork _uow;

    public CheckInService(
        IUserRepository users,
        IStreakRepository streaks,
        IParticipantRepository participants,
        ICheckInRepository checkIns,
        IUnitOfWork uow)
    {
        _users = users;
        _streaks = streaks;
        _participants = participants;
        _checkIns = checkIns;
        _uow = uow;
    }

    public async Task<CheckInResultDto> RecordAsync(string firebaseUid, Guid streakId, CancellationToken ct = default)
    {
        var user = await _users.GetByFirebaseUidAsync(firebaseUid, ct)
            ?? throw new NotFoundException("User not initialized.");

        if (await _streaks.GetByIdAsync(streakId, ct) is null)
            throw new NotFoundException("Streak not found.");

        if (!await _participants.ExistsAsync(user.Id, streakId, ct))
            throw new ForbiddenException("You are not a participant of this streak.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (await _checkIns.ExistsAsync(user.Id, streakId, today, ct))
            throw new ConflictException("Already checked in today.");

        await _checkIns.AddAsync(new CheckIn
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            StreakId = streakId,
            Date = today,
            CreatedAt = DateTime.UtcNow
        }, ct);
        await _uow.SaveChangesAsync(ct);

        var dates = await _checkIns.GetUserDatesAsync(user.Id, streakId, ct);
        var count = StreakCountCalculator.Compute(dates, today);
        return new CheckInResultDto(streakId, today, count);
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

        var participants = streak.Participants
            .OrderBy(p => p.JoinedAt)
            .Select(p =>
            {
                var dates = datesByUser.TryGetValue(p.UserId, out var d) ? d : Array.Empty<DateOnly>();
                return new ParticipantStatusDto(
                    p.UserId,
                    p.User.DisplayName ?? p.User.Email,
                    StreakCountCalculator.Compute(dates, today),
                    StreakCountCalculator.CheckedInToday(dates, today),
                    p.JoinedAt);
            })
            .ToList();

        return new StreakStatusDto(streakId, today, participants);
    }
}
