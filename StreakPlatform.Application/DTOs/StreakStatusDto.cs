namespace StreakPlatform.Application.DTOs;

public record StreakStatusDto(
    Guid StreakId,
    DateOnly Date,
    IReadOnlyList<ParticipantStatusDto> Participants);
