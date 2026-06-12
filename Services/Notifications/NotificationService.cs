using MediAlert.Data;
using MediAlert.DTOs.Notifications;
using MediAlert.Hubs;
using MediAlert.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediAlert.Services.Notifications;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _db;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly NotificationNotifier _notifier;

    public NotificationService(
        ApplicationDbContext db, 
        IHubContext<NotificationHub> hubContext,
        NotificationNotifier notifier)
    {
        _db = db;
        _hubContext = hubContext;
        _notifier = notifier;
    }

    public async Task SendNotificationAsync(string userId, string title, string message, string type, string? actionUrl = null, CancellationToken cancellationToken = default)
    {
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            ActionUrl = actionUrl,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync(cancellationToken);

        var response = new NotificationResponse
        {
            NotificationId = notification.NotificationId,
            Title = notification.Title,
            Message = notification.Message,
            Type = notification.Type,
            ActionUrl = notification.ActionUrl,
            IsRead = notification.IsRead,
            CreatedAt = notification.CreatedAt
        };

        // Send real-time update via SignalR to the specific user
        try
        {
            await _hubContext.Clients.Group(userId).SendAsync("ReceiveNotification", response, cancellationToken);
        }
        catch (Exception ex)
        {
            // Don't let SignalR failures block notification logging/in-memory dispatch
            System.Diagnostics.Debug.WriteLine($"SignalR hub broadcast failed: {ex.Message}");
        }

        // Also broadcast via in-memory notifier for the Blazor Server frontend
        _notifier.Notify(userId, response);
    }

    public async Task<List<NotificationResponse>> GetUnreadNotificationsAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _db.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationResponse
            {
                NotificationId = n.NotificationId,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type,
                ActionUrl = n.ActionUrl,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<NotificationResponse>> GetAllNotificationsAsync(string userId, int limit = 50, CancellationToken cancellationToken = default)
    {
        return await _db.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(limit)
            .Select(n => new NotificationResponse
            {
                NotificationId = n.NotificationId,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type,
                ActionUrl = n.ActionUrl,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task MarkAsReadAsync(Guid notificationId, string userId, CancellationToken cancellationToken = default)
    {
        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.UserId == userId, cancellationToken);

        if (notification != null && !notification.IsRead)
        {
            notification.IsRead = true;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task MarkAllAsReadAsync(string userId, CancellationToken cancellationToken = default)
    {
        var unread = await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(cancellationToken);

        if (unread.Any())
        {
            foreach (var n in unread)
            {
                n.IsRead = true;
            }
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
