namespace JunoBank.Application.Interfaces;

public interface INotificationService
{
    Task<NotificationPreference?> GetPreferenceAsync(int userId, NotificationType type);
    Task<NotificationPreference> UpsertPreferenceAsync(NotificationPreference preference);
    Task<int> ProcessDueNotificationsAsync();
}
