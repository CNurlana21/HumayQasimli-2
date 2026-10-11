using HumayQasimli.Data;
using HumayQasimli.Models.Entities;
using HumayQasimli.Models.ViewModels;
using HumayQasimli.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HumayQasimli.Areas.Admin.Controllers
{
    public class PortfolioController : AdminBaseController
    {
        private readonly ApplicationDbContext _db;
        private readonly IFileUploadService _fileUploadService;

        public PortfolioController(ApplicationDbContext db, IFileUploadService fileUploadService)
        {
            _db = db;
            _fileUploadService = fileUploadService;
        }

        public async Task<IActionResult> Index(string? search, int page = 1)
        {
            var query = _db.PortfolioProjects.Include(p => p.Category).AsQueryable();
            if (!string.IsNullOrWhiteSpace(search)) query = query.Where(p => p.Title.Contains(search));

            var total = await query.CountAsync();
            var items = await query.OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * DefaultPageSize).Take(DefaultPageSize).ToListAsync();

            return View(new PagedResult<PortfolioProject> { Items = items, PageNumber = page, PageSize = DefaultPageSize, TotalCount = total, SearchTerm = search });
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await PopulateCategories();
            return View(new PortfolioProject());
        }

        [HttpPost]
        public async Task<IActionResult> Create(PortfolioProject model, IFormFile? image)
        {
            if (!ModelState.IsValid) { await PopulateCategories(model.CategoryId); return View(model); }

            model.CreatedAt = DateTime.UtcNow;

            if (image != null)
            {
                var (ok, path, err) = await _fileUploadService.SaveFileAsync(image, "portfolio");
                if (!ok) { ModelState.AddModelError("", err!); await PopulateCategories(model.CategoryId); return View(model); }
                model.ImageUrl = path;
            }

            _db.PortfolioProjects.Add(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Layihe elave edildi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var item = await _db.PortfolioProjects.FindAsync(id);
            if (item == null) return NotFound();
            await PopulateCategories(item.CategoryId);
            return View(item);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, PortfolioProject model, IFormFile? image, bool removeImage)
        {
            if (id != model.Id) return NotFound();
            var item = await _db.PortfolioProjects.FindAsync(id);
            if (item == null) return NotFound();

            if (!ModelState.IsValid) { await PopulateCategories(model.CategoryId); return View(model); }

            item.Title = model.Title;
            item.Description = model.Description;
            item.ProjectUrl = model.ProjectUrl;
            item.GithubUrl = model.GithubUrl;
            item.TechStack = model.TechStack;
            item.CategoryId = model.CategoryId;
            item.IsFeatured = model.IsFeatured;
            item.DisplayOrder = model.DisplayOrder;

            if (removeImage)
            {
                _fileUploadService.DeleteFile(item.ImageUrl);
                item.ImageUrl = null;
            }
            else if (image != null)
            {
                var (ok, path, err) = await _fileUploadService.SaveFileAsync(image, "portfolio");
                if (!ok) { ModelState.AddModelError("", err!); await PopulateCategories(model.CategoryId); return View(model); }
                _fileUploadService.DeleteFile(item.ImageUrl);
                item.ImageUrl = path;
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = "Layihe yenilendi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _db.PortfolioProjects.FindAsync(id);
            if (item == null) return NotFound();
            _fileUploadService.DeleteFile(item.ImageUrl);
            _db.PortfolioProjects.Remove(item);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Layihe silindi.";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateCategories(int? selected = null)
        {
            ViewBag.Categories = new SelectList(await _db.Categories.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", selected);
        }
    }
}
