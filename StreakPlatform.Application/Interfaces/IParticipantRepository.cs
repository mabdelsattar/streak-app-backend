using StreakPlatform.Domain.Entities;

namespace StreakPlatform.Application.Interfaces;

public interface IParticipantRepository
{
    Task<bool> ExistsAsync(Guid userId, Guid streakId, CancellationToken ct = default);
    Task AddAsync(Participant participant, CancellationToken ct = default);
    Task<IReadOnlyList<Participant>> GetByStreakIdAsync(Guid streakId, CancellationToken ct = default);
}
