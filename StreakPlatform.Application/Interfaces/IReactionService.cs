using StreakPlatform.Application.DTOs;
using StreakPlatform.Domain.Entities;

namespace StreakPlatform.Application.Interfaces;

public interface IReactionService
{
    Task<ReactionResultDto> ReactAsync(string firebaseUid, Guid checkInId, ReactionType type, CancellationToken ct = default);
    Task<ReactionResultDto> RemoveAsync(string firebaseUid, Guid checkInId, CancellationToken ct = default);
}
