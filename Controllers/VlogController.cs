using HumayQasimli.Data;
using HumayQasimli.Models.Entities;
using HumayQasimli.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HumayQasimli.Controllers
{
    public class VlogController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationService _notificationService;

        public VlogController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, INotificationService notificationService)
        {
            _db = db;
            _userManager = userManager;
            _notificationService = notificationService;
        }

        public async Task<IActionResult> Index(int? categoryId, int page = 1)
        {
            const int pageSize = 9;

            var query = _db.Vlogs.Where(v => v.IsPublished).Include(v => v.Category).AsQueryable();

            if (categoryId.HasValue)
                query = query.Where(v => v.CategoryId == categoryId);

            var total = await query.CountAsync();

            var vlogs = await query
                .OrderByDescending(v => v.IsFeatured)
                .ThenByDescending(v => v.PublishDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Categories = await _db.Categories.OrderBy(c => c.Name).ToListAsync();
            ViewBag.SelectedCategoryId = categoryId;
            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);

            return View(vlogs);
        }

        public async Task<IActionResult> Details(int id)
        {
            var vlog = await _db.Vlogs
                .Include(v => v.Category)
                .Include(v => v.Comments.Where(c => !c.IsDeleted)).ThenInclude(c => c.User)
                .FirstOrDefaultAsync(v => v.Id == id && v.IsPublished);

            if (vlog == null) return NotFound();

            vlog.ViewCount += 1;
            await _db.SaveChangesAsync();

            var currentUserId = _userManager.GetUserId(User);

            ViewBag.LikeCount = await _db.Likes.CountAsync(l => l.VlogId == id);
            ViewBag.IsLiked = currentUserId != null && await _db.Likes.AnyAsync(l => l.VlogId == id && l.UserId == currentUserId);

            return View(vlog);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLike(int id)
        {
            var userId = _userManager.GetUserId(User)!;
            var vlog = await _db.Vlogs.FindAsync(id);
            if (vlog == null) return NotFound();

            var existing = await _db.Likes.FirstOrDefaultAsync(l => l.VlogId == id && l.UserId == userId);

            bool liked;
            if (existing != null)
            {
                _db.Likes.Remove(existing);
                liked = false;
            }
            else
            {
                _db.Likes.Add(new Like { VlogId = id, UserId = userId, CreatedAt = DateTime.UtcNow });
                liked = true;
            }

            await _db.SaveChangesAsync();

            var count = await _db.Likes.CountAsync(l => l.VlogId == id);
            return Json(new { success = true, liked, count });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int id, string content)
        {
            if (string.IsNullOrWhiteSpace(content) || content.Length > 2000)
            {
                return Json(new { success = false, error = "Serh bos ola bilmez ve 2000 simvoldan uzun ola bilmez." });
            }

            var vlog = await _db.Vlogs.FindAsync(id);
            if (vlog == null) return NotFound();

            var userId = _userManager.GetUserId(User)!;
            var user = await _userManager.FindByIdAsync(userId);

            var comment = new Comment
            {
                VlogId = id,
                UserId = userId,
                Content = content.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _db.Comments.Add(comment);
            await _db.SaveChangesAsync();

            return Json(new
            {
                success = true,
                userName = user?.FullName ?? "Istifadeci",
                content = comment.Content,
                createdAt = comment.CreatedAt.ToString("dd.MM.yyyy")
            });
        }

        public async Task<IActionResult> Share(int id)
        {
            var exists = await _db.Vlogs.AnyAsync(v => v.Id == id && v.IsPublished);
            if (!exists) return NotFound();

            var url = Url.Action("Details", "Vlog", new { id }, Request.Scheme);
            return Json(new { url });
        }
    }
}