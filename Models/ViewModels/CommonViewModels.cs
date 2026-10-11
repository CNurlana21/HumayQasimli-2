namespace HumayQasimli.Models.ViewModels
{
    /// <summary>Generic paged list wrapper used across admin list views.</summary>
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public string? SearchTerm { get; set; }
    }

    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalVlogs { get; set; }
        public int PublishedVlogs { get; set; }
        public int TotalPortfolioProjects { get; set; }
        public int TotalComments { get; set; }
        public int TotalLikes { get; set; }
        public int UnreadContactMessages { get; set; }
        public int TotalCertificates { get; set; }
        public int TotalTrainings { get; set; }
    }

    public class NotificationsPageViewModel
    {
        public List<HumayQasimli.Models.Entities.Notification> Unread { get; set; } = new();
        public List<HumayQasimli.Models.Entities.Notification> Read { get; set; } = new();
    }
}


