using HumayQasimli.Data;
using HumayQasimli.Models.Entities;
using HumayQasimli.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HumayQasimli.Areas.Admin.Controllers
{
    public class MentorReviewsController : AdminBaseController
    {
        private readonly ApplicationDbContext _db;
        public MentorReviewsController(ApplicationDbContext db) { _db = db; }

        public async Task<IActionResult> Index(string? search, int page = 1)
        {
            var query = _db.MentorReviews.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search)) query = query.Where(r => r.AuthorName.Contains(search));
            var total = await query.CountAsync();
            var items = await query.OrderBy(r => r.DisplayOrder).ThenByDescending(r => r.CreatedAt).Skip((page - 1) * DefaultPageSize).Take(DefaultPageSize).ToListAsync();
            return View(new PagedResult<MentorReview> { Items = items, PageNumber = page, PageSize = DefaultPageSize, TotalCount = total, SearchTerm = search });
        }

        [HttpGet] public IActionResult Create() => View(new MentorReview());

        [HttpPost]
        public async Task<IActionResult> Create(MentorReview model)
        {
            if (!ModelState.IsValid) return View(model);
            model.CreatedAt = DateTime.UtcNow;
            _db.MentorReviews.Add(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Rey elave edildi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var item = await _db.MentorReviews.FindAsync(id);
            if (item == null) return NotFound();
            return View(item);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, MentorReview model)
        {
            if (id != model.Id) return NotFound();
            if (!ModelState.IsValid) return View(model);
            var item = await _db.MentorReviews.FindAsync(id);
            if (item == null) return NotFound();

            item.AuthorName = model.AuthorName; item.AuthorTitle = model.AuthorTitle; item.ProjectName = model.ProjectName;
            item.Content = model.Content; item.IsActive = model.IsActive; item.DisplayOrder = model.DisplayOrder;

            await _db.SaveChangesAsync();
            TempData["Success"] = "Rey yenilendi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _db.MentorReviews.FindAsync(id);
            if (item == null) return NotFound();
            _db.MentorReviews.Remove(item);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Rey silindi.";
            return RedirectToAction(nameof(Index));
        }
    }
}