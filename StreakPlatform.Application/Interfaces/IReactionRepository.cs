using StreakPlatform.Domain.Entities;

namespace StreakPlatform.Application.Interfaces;

public interface IReactionRepository
{
    Task<CheckInReaction?> GetAsync(Guid checkInId, Guid reactorUserId, CancellationToken ct = default);
    Task AddAsync(CheckInReaction reaction, CancellationToken ct = default);
    void Remove(CheckInReaction reaction);
    Task<(int Likes, int Dislikes)> CountAsync(Guid checkInId, CancellationToken ct = default);
    Task<IReadOnlyDictionary<Guid, (int Likes, int Dislikes)>> CountByCheckInAsync(IEnumerable<Guid> checkInIds, CancellationToken ct = default);
    Task<IReadOnlyDictionary<Guid, ReactionType>> GetMyReactionsByCheckInAsync(IEnumerable<Guid> checkInIds, Guid reactorUserId, CancellationToken ct = default);
}
