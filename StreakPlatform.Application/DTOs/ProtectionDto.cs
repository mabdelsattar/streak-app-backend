namespace StreakPlatform.Application.DTOs;

public record ProtectionDto(
    Guid Id,
    Guid StreakId,
    string Status,        // "Pending" | "Used" | "Cancelled"
    int PointsCost,
    DateTime ScheduledAt,
    DateOnly? AppliedToDate);
