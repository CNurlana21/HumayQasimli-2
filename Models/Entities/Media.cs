namespace HumayQasimli.Models.Entities
{
    public class Media
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string? UploadedByUserId { get; set; }
        public ApplicationUser? UploadedByUser { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}
