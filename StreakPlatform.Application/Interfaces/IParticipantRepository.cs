using StreakPlatform.Domain.Entities;

namespace StreakPlatform.Application.Interfaces;

public interface IParticipantRepository
{
    /// <summary>Returns true only when there's an ACTIVE participant row for (user, streak).</summary>
    Task<bool> IsActiveAsync(Guid userId, Guid streakId, CancellationToken ct = default);

    /// <summary>Returns the participant row regardless of IsActive — used by join flow to decide reactivate vs insert.</summary>
    Task<Participant?> GetAsync(Guid userId, Guid streakId, CancellationToken ct = default);

    Task AddAsync(Participant participant, CancellationToken ct = default);
    Task<IReadOnlyList<Participant>> GetByStreakIdAsync(Guid streakId, CancellationToken ct = default);
}
