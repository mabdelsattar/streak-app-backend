namespace StreakPlatform.Application.DTOs;

public record ParticipantStatusDto(
    Guid UserId,
    string DisplayName,
    int CurrentCount,
    bool CheckedInToday,
    DateTime JoinedAt);
