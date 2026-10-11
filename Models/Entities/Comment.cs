namespace HumayQasimli.Models.Entities
{
    public class Comment
    {
        public int Id { get; set; }
        public int VlogId { get; set; }
        public Vlog? Vlog { get; set; }
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; } = false;

        public string? AdminReply { get; set; }
        public DateTime? RepliedAt { get; set; }
    }
}