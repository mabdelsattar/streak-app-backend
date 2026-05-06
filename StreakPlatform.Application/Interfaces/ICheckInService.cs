using StreakPlatform.Application.DTOs;

namespace StreakPlatform.Application.Interfaces;

public interface ICheckInService
{
    Task<CheckInResultDto> RecordAsync(string firebaseUid, Guid streakId, CheckInRequest req, CancellationToken ct = default);
    Task<TodayStatusDto> GetTodayStatusAsync(string firebaseUid, Guid streakId, CancellationToken ct = default);
    Task<StreakStatusDto> GetStreakStatusAsync(string firebaseUid, Guid streakId, CancellationToken ct = default);
    Task<IReadOnlyList<CheckInFeedItemDto>> GetFeedAsync(string firebaseUid, Guid streakId, int take, int skip, CancellationToken ct = default);
}
