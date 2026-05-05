namespace StreakPlatform.Application.DTOs;

public record TodayParticipantDto(Guid UserId, string DisplayName, bool CheckedInToday);

public record TodayStatusDto(Guid StreakId, DateOnly Date, IReadOnlyList<TodayParticipantDto> Participants);
