namespace HumayQasimli.Models.Entities
{
    public class MentorReview
    {
        public int Id { get; set; }
        public string AuthorName { get; set; } = string.Empty;   // Ad Soyad
        public string? AuthorTitle { get; set; }                  // Status (məs. Tələbə, Menecer, Sahibkar)
        public string? ProjectName { get; set; }                  // Qoşulduğu layihə
        public string Content { get; set; } = string.Empty;       // Rəy mətni
        public bool IsActive { get; set; } = true;
        public int DisplayOrder { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}