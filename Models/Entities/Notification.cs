namespace HumayQasimli.Models.Entities
{
    public enum NotificationType
    {
        Like,
        Comment,
        Follow,
        AdminReply,
        System
    }

    public class Notification
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }
        public NotificationType Type { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Url { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
