using HumayQasimli.Models.Entities;

namespace HumayQasimli.Services
{
    public interface INotificationService
    {
        Task CreateAsync(string userId, NotificationType type, string message, string? url = null);
        Task<int> GetUnreadCountAsync(string userId);
        Task MarkAsReadAsync(int notificationId, string userId);
        Task MarkAllAsReadAsync(string userId);
        Task DeleteAsync(int notificationId, string userId);
    }
}

