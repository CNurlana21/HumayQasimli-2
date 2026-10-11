namespace HumayQasimli.Models.Entities
{
    public class ContactMessage
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Subject { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Set only when the sender was logged in when they submitted the
        /// form - lets an admin reply trigger a Notification for that user.</summary>
        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }

        public string? AdminReply { get; set; }
        public DateTime? RepliedAt { get; set; }
    }
}