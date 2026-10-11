namespace HumayQasimli.Models.Entities
{
    public class Like
    {
        public int Id { get; set; }
        public int VlogId { get; set; }
        public Vlog? Vlog { get; set; }
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
