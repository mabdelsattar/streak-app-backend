namespace StreakPlatform.Application.Interfaces;

// Future-ready placeholder; FCM integration in a later phase.
public interface INotificationService
{
    Task SendCheckInReminderAsync(Guid userId, Guid streakId, CancellationToken ct = default);
}
