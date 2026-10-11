namespace HumayQasimli.Models.Entities
{
    public class Certificate
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string IssuedBy { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public DateTime? IssueDate { get; set; }
        public string? CredentialUrl { get; set; }
        public int DisplayOrder { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
