using HumayQasimli.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HumayQasimli.Controllers
{
    public class BlogController : Controller
    {
        private readonly ApplicationDbContext _db;

        public BlogController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index(int? categoryId, int page = 1)
        {
            const int pageSize = 9;

            var query = _db.Blogs.Where(b => b.IsPublished).Include(b => b.Category).AsQueryable();

            if (categoryId.HasValue)
                query = query.Where(b => b.CategoryId == categoryId);

            var total = await query.CountAsync();

            var blogs = await query
                .OrderByDescending(b => b.PublishDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Categories = await _db.Categories.OrderBy(c => c.Name).ToListAsync();
            ViewBag.SelectedCategoryId = categoryId;
            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);

            return View(blogs);
        }

        public async Task<IActionResult> Details(int id)
        {
            var blog = await _db.Blogs
                .Include(b => b.Category)
                .FirstOrDefaultAsync(b => b.Id == id && b.IsPublished);

            if (blog == null) return NotFound();

            return View(blog);
        }
    }
}