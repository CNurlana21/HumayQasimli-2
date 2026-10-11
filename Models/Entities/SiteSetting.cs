namespace HumayQasimli.Models.Entities
{
    public class SiteSetting
    {
        public int Id { get; set; }
        public string SiteName { get; set; } = "Humay Qasimli";
        public string? HeroTitle { get; set; }
        public string? HeroSubtitle { get; set; }
        public string? ProfileImageUrl { get; set; }
        public string? AboutTitle { get; set; }
        public string? AboutContent { get; set; }
        public string? InstagramUrl { get; set; }
        public string? LinkedInUrl { get; set; }
        public string? WhatsAppNumber { get; set; }
        public string? YoutubeUrl { get; set; }
        public string? ContactEmail { get; set; }
        public string? FooterText { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
