using HumayQasimli.Models.Entities;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;


namespace HumayQasimli.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IDataProtectionKeyContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Vlog> Vlogs => Set<Vlog>();
        public DbSet<Blog> Blogs => Set<Blog>();
        public DbSet<Comment> Comments => Set<Comment>();
        public DbSet<Like> Likes => Set<Like>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();
        public DbSet<MentorMeeting> MentorMeetings => Set<MentorMeeting>();
        
        public DbSet<Training> Trainings => Set<Training>();
        public DbSet<Certificate> Certificates => Set<Certificate>();
        public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;
        public DbSet<MentorReview> MentorReviews => Set<MentorReview>();
        public DbSet<PortfolioProject> PortfolioProjects => Set<PortfolioProject>();
        public DbSet<Media> MediaItems => Set<Media>();
        public DbSet<SiteSetting> SiteSettings => Set<SiteSetting>();
        public DbSet<EmailVerificationOtp> EmailVerificationOtps => Set<EmailVerificationOtp>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Category>(e =>
            {
                e.HasIndex(c => c.Slug).IsUnique();
                e.Property(c => c.Name).HasMaxLength(150).IsRequired();
                e.Property(c => c.Slug).HasMaxLength(180).IsRequired();
            });

            builder.Entity<Vlog>(e =>
            {
                e.HasIndex(v => v.Slug).IsUnique();
                e.Property(v => v.Title).HasMaxLength(250).IsRequired();
                e.Property(v => v.VideoUrl).HasMaxLength(500).IsRequired();

                e.HasOne(v => v.Category)
             .WithMany(c => c.Vlogs)
             .HasForeignKey(v => v.CategoryId)
             .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Blog>(e =>
            {
                e.HasIndex(b => b.Slug).IsUnique();
                e.Property(b => b.Title).HasMaxLength(250).IsRequired();
                e.Property(b => b.ShortDescription).HasMaxLength(500).IsRequired();
                e.Property(b => b.Content).IsRequired();

                e.HasOne(b => b.Category)
                 .WithMany(c => c.Blogs)
                 .HasForeignKey(b => b.CategoryId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Comment>(e =>
            {
                e.Property(c => c.Content).HasMaxLength(2000).IsRequired();

                e.HasOne(c => c.Vlog)
                 .WithMany(v => v.Comments)
                 .HasForeignKey(c => c.VlogId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(c => c.User)
                 .WithMany(u => u.Comments)
                 .HasForeignKey(c => c.UserId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Like>(e =>
            {
                e.HasIndex(l => new { l.VlogId, l.UserId }).IsUnique();

                e.HasOne(l => l.Vlog)
                 .WithMany(v => v.Likes)
                 .HasForeignKey(l => l.VlogId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(l => l.User)
                 .WithMany(u => u.Likes)
                 .HasForeignKey(l => l.UserId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Notification>(e =>
            {
                e.Property(n => n.Message).HasMaxLength(500).IsRequired();

                e.HasOne(n => n.User)
                 .WithMany(u => u.Notifications)
                 .HasForeignKey(n => n.UserId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<ContactMessage>(e =>
            {
                e.Property(c => c.Name).HasMaxLength(150).IsRequired();
                e.Property(c => c.Email).HasMaxLength(200).IsRequired();
                e.Property(c => c.Message).HasMaxLength(4000).IsRequired();

                e.HasOne(c => c.User)
                 .WithMany()
                 .HasForeignKey(c => c.UserId)
                 .OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<PortfolioProject>(e =>
            {
                e.Property(p => p.Title).HasMaxLength(250).IsRequired();

                e.HasOne(p => p.Category)
                 .WithMany(c => c.PortfolioProjects)
                 .HasForeignKey(p => p.CategoryId)
                 .OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<Media>(e =>
            {
                e.Property(m => m.FileName).HasMaxLength(300).IsRequired();
                e.Property(m => m.FilePath).HasMaxLength(500).IsRequired();

                e.HasOne(m => m.UploadedByUser)
                 .WithMany()
                 .HasForeignKey(m => m.UploadedByUserId)
                 .OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<EmailVerificationOtp>(e =>
            {
                e.Property(o => o.Code).HasMaxLength(10).IsRequired();

                e.HasOne(o => o.User)
                 .WithMany()
                 .HasForeignKey(o => o.UserId)
                 .OnDelete(DeleteBehavior.Cascade);
            });
            builder.Entity<MentorReview>(e =>
            {
                e.Property(r => r.AuthorName).HasMaxLength(200).IsRequired();
                e.Property(r => r.AuthorTitle).HasMaxLength(200);
                e.Property(r => r.ProjectName).HasMaxLength(250);
                e.Property(r => r.Content).HasMaxLength(4000).IsRequired();
            });
            builder.Entity<MentorMeeting>(e => e.Property(m => m.Title).HasMaxLength(250).IsRequired());
           
            builder.Entity<Training>(e => e.Property(t => t.Title).HasMaxLength(250).IsRequired());
            builder.Entity<Certificate>(e => e.Property(c => c.Title).HasMaxLength(250).IsRequired());

            builder.Entity<SiteSetting>(e => e.Property(s => s.SiteName).HasMaxLength(200).IsRequired());
        }
    }
}