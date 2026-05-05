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
    private readonly IUnitOfWork _uow;
    private readonly InviteCodeGenerator _codes;
    private readonly InviteUrlBuilder _urlBuilder;

    public StreakService(
        IUserRepository users,
        IStreakRepository streaks,
        IParticipantRepository participants,
        ICheckInRepository checkIns,
        IUnitOfWork uow,
        InviteCodeGenerator codes,
        InviteUrlBuilder urlBuilder)
    {
        _users = users;
        _streaks = streaks;
        _participants = participants;
        _checkIns = checkIns;
        _uow = uow;
        _codes = codes;
        _urlBuilder = urlBuilder;
    }

    public async Task<StreakDetailDto> CreateAsync(string firebaseUid, CreateStreakRequest req, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            throw new ValidationException("Streak name is required.");

        var user = await GetUserOrThrow(firebaseUid, ct);
        var now = DateTime.UtcNow;
        var streak = new Streak
        {
            Id = Guid.NewGuid(),
            Name = req.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim(),
            CreatedBy = user.Id,
            InviteCode = await _codes.GenerateUniqueAsync(ct),
            CreatedAt = now
        };
        await _streaks.AddAsync(streak, ct);

        // Creator auto-joins as participant.
        await _participants.AddAsync(new Participant
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            StreakId = streak.Id,
            JoinedAt = now
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
            summaries.Add(new StreakSummaryDto(
                s.Id,
                s.Name,
                s.Description,
                StreakCountCalculator.Compute(dates, today),
                StreakCountCalculator.CheckedInToday(dates, today),
                s.Participants.Count));
        }
        return summaries;
    }

    public async Task<StreakDetailDto> GetDetailAsync(string firebaseUid, Guid streakId, CancellationToken ct = default)
    {
        var user = await GetUserOrThrow(firebaseUid, ct);
        await EnsureParticipantOrThrow(user.Id, streakId, ct);
        return await BuildDetailAsync(user.Id, streakId, ct);
    }

    public async Task<StreakDetailDto> JoinByInviteCodeAsync(string firebaseUid, string inviteCode, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(inviteCode))
            throw new ValidationException("Invite code is required.");

        var user = await GetUserOrThrow(firebaseUid, ct);
        var streak = await _streaks.GetByInviteCodeAsync(inviteCode.Trim().ToUpperInvariant(), ct)
            ?? throw new NotFoundException("Invalid invite code.");

        if (await _participants.ExistsAsync(user.Id, streak.Id, ct))
            throw new ConflictException("You have already joined this streak.");

        await _participants.AddAsync(new Participant
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            StreakId = streak.Id,
            JoinedAt = DateTime.UtcNow
        }, ct);
        await _uow.SaveChangesAsync(ct);

        return await BuildDetailAsync(user.Id, streak.Id, ct);
    }

    public async Task<InviteDto> GetInviteAsync(string firebaseUid, Guid streakId, CancellationToken ct = default)
    {
        var user = await GetUserOrThrow(firebaseUid, ct);
        await EnsureParticipantOrThrow(user.Id, streakId, ct);
        var streak = await _streaks.GetByIdAsync(streakId, ct)
            ?? throw new NotFoundException("Streak not found.");
        return new InviteDto(streak.Id, streak.InviteCode, _urlBuilder.Build(streak.InviteCode));
    }

    private async Task<User> GetUserOrThrow(string firebaseUid, CancellationToken ct)
    {
        return await _users.GetByFirebaseUidAsync(firebaseUid, ct)
            ?? throw new NotFoundException("User not initialized.");
    }

    private async Task EnsureParticipantOrThrow(Guid userId, Guid streakId, CancellationToken ct)
    {
        if (!await _participants.ExistsAsync(userId, streakId, ct))
            throw new ForbiddenException("You are not a participant of this streak.");
    }

    private async Task<StreakDetailDto> BuildDetailAsync(Guid currentUserId, Guid streakId, CancellationToken ct)
    {
        var streak = await _streaks.GetByIdWithParticipantsAsync(streakId, ct)
            ?? throw new NotFoundException("Streak not found.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var userIds = streak.Participants.Select(p => p.UserId).ToList();
        var datesByUser = await _checkIns.GetDatesByUsersAsync(userIds, streak.Id, ct);

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

        var me = participants.First(x => x.UserId == currentUserId);

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
            participants);
    }
}
