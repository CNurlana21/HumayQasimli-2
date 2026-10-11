using HumayQasimli.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HumayQasimli.Controllers
{
    public class PortfolioController : Controller
    {
        private readonly ApplicationDbContext _db;

        public PortfolioController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index(int? categoryId)
        {
            var query = _db.PortfolioProjects.Include(p => p.Category).AsQueryable();

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId);
            }

            var projects = await query
                .OrderByDescending(p => p.IsFeatured)
                .ThenBy(p => p.DisplayOrder)
                .ToListAsync();

            ViewBag.Categories = await _db.Categories.OrderBy(c => c.Name).ToListAsync();
            ViewBag.SelectedCategoryId = categoryId;

            return View(projects);
        }

        public async Task<IActionResult> Details(int id)
        {
            var project = await _db.PortfolioProjects
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null) return NotFound();
            return View(project);
        }
    }
}
