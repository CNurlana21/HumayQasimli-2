namespace HumayQasimli.Models.Entities
{
    /// <summary>
    /// ExternalLink: VideoUrl is a normal link to a social platform (YouTube,
    /// Instagram, LinkedIn, TikTok, etc.) - the site shows a "watch on X" card
    /// that opens the link in a new tab, since these platforms generally can't
    /// be embedded with a plain iframe.
    /// UploadedVideo: the video file itself was uploaded and is stored on this
    /// server (wwwroot/uploads/vlogs/videos/...); VideoUrl holds that local path
    /// and the site plays it directly with a native &lt;video&gt; player.
    /// </summary>
    public enum VideoSourceType
    {
        ExternalLink = 0,
        UploadedVideo = 1
    }

    public class Vlog
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? CoverImageUrl { get; set; }
        public string VideoUrl { get; set; } = string.Empty;
        public VideoSourceType VideoSourceType { get; set; } = VideoSourceType.ExternalLink;
        public int CategoryId { get; set; }
        public Category? Category { get; set; }
        public DateTime PublishDate { get; set; } = DateTime.UtcNow;
        public bool IsFeatured { get; set; } = false;
        public bool IsPublished { get; set; } = false;
        public int ViewCount { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<Like> Likes { get; set; } = new List<Like>();
    }
}