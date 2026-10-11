using HumayQasimli.Data;
using HumayQasimli.Models.Entities;
using HumayQasimli.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HumayQasimli.Areas.Admin.Controllers
{
    public class CategoriesController : AdminBaseController
    {
        private readonly ApplicationDbContext _db;

        public CategoriesController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index(string? search, int page = 1)
        {
            var query = _db.Categories.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(c => c.Name.Contains(search));

            var total = await query.CountAsync();
            var items = await query.OrderBy(c => c.Name)
                .Skip((page - 1) * DefaultPageSize).Take(DefaultPageSize).ToListAsync();

            return View(new PagedResult<Category>
            {
                Items = items,
                PageNumber = page,
                PageSize = DefaultPageSize,
                TotalCount = total,
                SearchTerm = search
            });
        }

        [HttpGet]
        public IActionResult Create() => View(new Category());

        [HttpPost]
        public async Task<IActionResult> Create(Category model)
        {
            if (!ModelState.IsValid) return View(model);

            model.Slug = Slugify(model.Name);
            model.CreatedAt = DateTime.UtcNow;

            _db.Categories.Add(model);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Kateqoriya elave edildi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _db.Categories.FindAsync(id);
            if (category == null) return NotFound();
            return View(category);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, Category model)
        {
            if (id != model.Id) return NotFound();
            if (!ModelState.IsValid) return View(model);

            var category = await _db.Categories.FindAsync(id);
            if (category == null) return NotFound();

            category.Name = model.Name;
            category.Slug = Slugify(model.Name);
            category.Description = model.Description;

            await _db.SaveChangesAsync();
            TempData["Success"] = "Kateqoriya yenilendi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _db.Categories.FindAsync(id);
            if (category == null) return NotFound();

            _db.Categories.Remove(category);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Kateqoriya silindi.";
            return RedirectToAction(nameof(Index));
        }

        private static string Slugify(string name)
        {
            var s = name.ToLowerInvariant().Trim();
            s = s.Replace("ə", "e").Replace("ı", "i").Replace("ö", "o").Replace("ü", "u")
                 .Replace("ç", "c").Replace("ş", "s").Replace("ğ", "g");
            s = System.Text.RegularExpressions.Regex.Replace(s, @"[^a-z0-9\s-]", "");
            s = System.Text.RegularExpressions.Regex.Replace(s, @"\s+", "-");
            return s.Trim('-');
        }
    }
}
