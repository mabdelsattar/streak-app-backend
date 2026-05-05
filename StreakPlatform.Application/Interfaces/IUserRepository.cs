using StreakPlatform.Domain.Entities;

namespace StreakPlatform.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByFirebaseUidAsync(string firebaseUid, CancellationToken ct = default);
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
}
