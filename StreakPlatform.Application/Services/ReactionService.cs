using Microsoft.Extensions.Options;
using StreakPlatform.Application.Common;
using StreakPlatform.Application.DTOs;
using StreakPlatform.Application.Interfaces;
using StreakPlatform.Domain.Entities;

namespace StreakPlatform.Application.Services;

public class ReactionService : IReactionService
{
    private readonly IUserRepository _users;
    private readonly ICheckInRepository _checkIns;
    private readonly IParticipantRepository _participants;
    private readonly IReactionRepository _reactions;
    private readonly IPointsService _points;
    private readonly IUnitOfWork _uow;
    private readonly AppOptions _options;

    public ReactionService(
        IUserRepository users,
        ICheckInRepository checkIns,
        IParticipantRepository participants,
        IReactionRepository reactions,
        IPointsService points,
        IUnitOfWork uow,
        IOptions<AppOptions> options)
    {
        _users = users;
        _checkIns = checkIns;
        _participants = participants;
        _reactions = reactions;
        _points = points;
        _uow = uow;
        _options = options.Value;
    }

    public async Task<ReactionResultDto> ReactAsync(string firebaseUid, Guid checkInId, ReactionType type, CancellationToken ct = default)
    {
        var (reactor, checkIn, author) = await LoadAndAuthorize(firebaseUid, checkInId, ct);

        var existing = await _reactions.GetAsync(checkInId, reactor.Id, ct);

        if (existing is not null && existing.Type == type)
        {
            // Idempotent — return current state without changes.
            var (l, d) = await _reactions.CountAsync(checkInId, ct);
            return new ReactionResultDto(checkInId, type.ToString(), l, d, reactor.PointsBalance, author.PointsBalance);
        }

        var like = _options.ReactionLikePoints;
        var dislike = _options.ReactionDislikePoints;
        var giverReason = PointsTransactionReason.ReactionGiven;
        var likeReason = PointsTransactionReason.ReactionReceivedLike;
        var dislikeReason = PointsTransactionReason.ReactionReceivedDislike;
        var streakId = checkIn.StreakId;

        int reactorBalance = reactor.PointsBalance;
        int authorBalance = author.PointsBalance;

        if (existing is null)
        {
            // First reaction: reactor +5; author +5 (Like) or -5 (Dislike).
            reactorBalance = await _points.AwardAsync(reactor.Id, +like, giverReason, streakId, null, ct);
            authorBalance = type == ReactionType.Like
                ? await _points.AwardAsync(author.Id, +like, likeReason, streakId, null, ct)
                : await _points.AwardAsync(author.Id, -dislike, dislikeReason, streakId, null, ct);

            await _reactions.AddAsync(new CheckInReaction
            {
                Id = Guid.NewGuid(),
                CheckInId = checkInId,
                ReactorUserId = reactor.Id,
                Type = type,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }, ct);
        }
        else
        {
            // Switching from one to the other: reactor delta 0; author flip.
            if (existing.Type == ReactionType.Like && type == ReactionType.Dislike)
            {
                authorBalance = await _points.AwardAsync(author.Id, -like, likeReason, streakId, null, ct);          // refund the +like
                authorBalance = await _points.AwardAsync(author.Id, -dislike, dislikeReason, streakId, null, ct);   // apply dislike
            }
            else if (existing.Type == ReactionType.Dislike && type == ReactionType.Like)
            {
                authorBalance = await _points.AwardAsync(author.Id, +dislike, dislikeReason, streakId, null, ct);   // refund the -dislike (clamped)
                authorBalance = await _points.AwardAsync(author.Id, +like, likeReason, streakId, null, ct);         // apply like
            }
            existing.Type = type;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _uow.SaveChangesAsync(ct);

        var (likes, dislikes) = await _reactions.CountAsync(checkInId, ct);
        return new ReactionResultDto(checkInId, type.ToString(), likes, dislikes, reactorBalance, authorBalance);
    }

    public async Task<ReactionResultDto> RemoveAsync(string firebaseUid, Guid checkInId, CancellationToken ct = default)
    {
        var (reactor, checkIn, author) = await LoadAndAuthorize(firebaseUid, checkInId, ct);

        var existing = await _reactions.GetAsync(checkInId, reactor.Id, ct);
        int reactorBalance = reactor.PointsBalance;
        int authorBalance = author.PointsBalance;

        if (existing is not null)
        {
            var like = _options.ReactionLikePoints;
            var dislike = _options.ReactionDislikePoints;
            var streakId = checkIn.StreakId;

            // Reverse reactor's grant.
            reactorBalance = await _points.AwardAsync(reactor.Id, -like,
                PointsTransactionReason.ReactionGiven, streakId, null, ct);

            // Reverse author's effect.
            authorBalance = existing.Type == ReactionType.Like
                ? await _points.AwardAsync(author.Id, -like, PointsTransactionReason.ReactionReceivedLike, streakId, null, ct)
                : await _points.AwardAsync(author.Id, +dislike, PointsTransactionReason.ReactionReceivedDislike, streakId, null, ct);

            _reactions.Remove(existing);
            await _uow.SaveChangesAsync(ct);
        }

        var (likes, dislikes) = await _reactions.CountAsync(checkInId, ct);
        return new ReactionResultDto(checkInId, null, likes, dislikes, reactorBalance, authorBalance);
    }

    private async Task<(User Reactor, CheckIn CheckIn, User Author)> LoadAndAuthorize(string firebaseUid, Guid checkInId, CancellationToken ct)
    {
        var reactor = await _users.GetByFirebaseUidAsync(firebaseUid, ct)
            ?? throw new NotFoundException("User not initialized.");

        var checkIn = await _checkIns.GetByIdAsync(checkInId, ct)
            ?? throw new NotFoundException("Check-in not found.");

        if (checkIn.UserId == reactor.Id)
            throw new ForbiddenException("You can't react to your own check-in.");

        if (!await _participants.ExistsAsync(reactor.Id, checkIn.StreakId, ct))
            throw new ForbiddenException("You are not a participant of this streak.");

        var author = await _users.GetByIdAsync(checkIn.UserId, ct)
            ?? throw new NotFoundException("Check-in author not found.");

        return (reactor, checkIn, author);
    }
}
