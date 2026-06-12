using MediAlert.DTOs.Notifications;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediAlert.Services.Notifications;

public interface INotificationService
{
    Task SendNotificationAsync(string userId, string title, string message, string type, string? actionUrl = null, CancellationToken cancellationToken = default);
    Task<List<NotificationResponse>> GetUnreadNotificationsAsync(string userId, CancellationToken cancellationToken = default);
    Task<List<NotificationResponse>> GetAllNotificationsAsync(string userId, int limit = 50, CancellationToken cancellationToken = default);
    Task MarkAsReadAsync(Guid notificationId, string userId, CancellationToken cancellationToken = default);
    Task MarkAllAsReadAsync(string userId, CancellationToken cancellationToken = default);
}
