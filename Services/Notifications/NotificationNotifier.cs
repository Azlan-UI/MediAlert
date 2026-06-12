using System;
using MediAlert.DTOs.Notifications;

namespace MediAlert.Services.Notifications;

public class NotificationNotifier
{
    public event Action<string, NotificationResponse>? OnNotificationReceived;

    public void Notify(string userId, NotificationResponse notification)
    {
        OnNotificationReceived?.Invoke(userId, notification);
    }
}
