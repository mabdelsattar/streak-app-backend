namespace StreakPlatform.Application.DTOs;

public record CheckInResultDto(Guid StreakId, DateOnly Date, int CurrentCount);
