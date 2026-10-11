namespace HumayQasimli.Models.Entities
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Vlog> Vlogs { get; set; } = new List<Vlog>();
        public ICollection<PortfolioProject> PortfolioProjects { get; set; } = new List<PortfolioProject>();
        public ICollection<Blog> Blogs { get; set; } = new List<Blog>();
    }
}
