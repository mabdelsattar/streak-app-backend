namespace StreakPlatform.Application.Services;

public static class StreakCountCalculator
{
    /// <summary>
    /// Computes the current consecutive-day count ending today or yesterday.
    /// Yesterday is allowed so a streak isn't shown as broken until midnight passes.
    /// </summary>
    public static int Compute(IEnumerable<DateOnly> dates, DateOnly today) =>
        Compute(dates, Array.Empty<DateOnly>(), today);

    /// <summary>
    /// Protection-aware overload: a paid-for "protected day" counts toward chain continuity.
    /// </summary>
    public static int Compute(IEnumerable<DateOnly> checkIns, IEnumerable<DateOnly> protectedDays, DateOnly today)
    {
        var set = new HashSet<DateOnly>(checkIns);
        set.UnionWith(protectedDays);
        if (set.Count == 0) return 0;

        DateOnly cursor;
        if (set.Contains(today))
            cursor = today;
        else if (set.Contains(today.AddDays(-1)))
            cursor = today.AddDays(-1);
        else
            return 0;

        var count = 0;
        while (set.Contains(cursor))
        {
            count++;
            cursor = cursor.AddDays(-1);
        }
        return count;
    }

    public static bool CheckedInToday(IEnumerable<DateOnly> dates, DateOnly today) =>
        dates.Any(d => d == today);
}
