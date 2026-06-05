namespace StreakPlatform.Application.DTOs;

public record UserProfileDto(
    Guid Id,
    string Email,
    string? DisplayName,
    int PointsBalance,
    bool NeedsToBuyPoints);
