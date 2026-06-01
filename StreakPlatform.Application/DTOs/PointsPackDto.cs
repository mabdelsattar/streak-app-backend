namespace StreakPlatform.Application.DTOs;

public record PointsPackDto(
    string Id,
    int Points,
    int PriceUsdCents,
    string Label,
    bool IsPopular);
