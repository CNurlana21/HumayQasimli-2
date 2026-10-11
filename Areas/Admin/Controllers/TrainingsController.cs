using HumayQasimli.Data;
using HumayQasimli.Models.Entities;
using HumayQasimli.Models.ViewModels;
using HumayQasimli.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HumayQasimli.Areas.Admin.Controllers
{
    public class TrainingsController : AdminBaseController
    {
        private readonly ApplicationDbContext _db;
        private readonly IFileUploadService _fileUploadService;
        public TrainingsController(ApplicationDbContext db, IFileUploadService fileUploadService) { _db = db; _fileUploadService = fileUploadService; }

        public async Task<IActionResult> Index(string? search, int page = 1)
        {
            var query = _db.Trainings.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search)) query = query.Where(t => t.Title.Contains(search));
            var total = await query.CountAsync();
            var items = await query.OrderByDescending(t => t.StartDate).Skip((page - 1) * DefaultPageSize).Take(DefaultPageSize).ToListAsync();
            return View(new PagedResult<Training> { Items = items, PageNumber = page, PageSize = DefaultPageSize, TotalCount = total, SearchTerm = search });
        }

        [HttpGet] public IActionResult Create() => View(new Training());

        [HttpPost]
        public async Task<IActionResult> Create(Training model, IFormFile? image)
        {
            if (!ModelState.IsValid) return View(model);
            model.CreatedAt = DateTime.UtcNow;

            if (image != null)
            {
                var (ok, path, err) = await _fileUploadService.SaveFileAsync(image, "trainings");
                if (!ok) { ModelState.AddModelError("", err!); return View(model); }
                model.ImageUrl = path;
            }

            _db.Trainings.Add(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Telim elave edildi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var item = await _db.Trainings.FindAsync(id);
            if (item == null) return NotFound();
            return View(item);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, Training model, IFormFile? image, bool removeImage)
        {
            if (id != model.Id) return NotFound();
            if (!ModelState.IsValid) return View(model);
            var item = await _db.Trainings.FindAsync(id);
            if (item == null) return NotFound();

            item.Title = model.Title; item.Description = model.Description; item.StartDate = model.StartDate;
            item.RegistrationUrl = model.RegistrationUrl; item.IsActive = model.IsActive;

            if (removeImage)
            {
                _fileUploadService.DeleteFile(item.ImageUrl);
                item.ImageUrl = null;
            }
            else if (image != null)
            {
                var (ok, path, err) = await _fileUploadService.SaveFileAsync(image, "trainings");
                if (!ok) { ModelState.AddModelError("", err!); return View(model); }
                _fileUploadService.DeleteFile(item.ImageUrl);
                item.ImageUrl = path;
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = "Telim yenilendi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _db.Trainings.FindAsync(id);
            if (item == null) return NotFound();
            _fileUploadService.DeleteFile(item.ImageUrl);
            _db.Trainings.Remove(item);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Telim silindi.";
            return RedirectToAction(nameof(Index));
        }
    }
}
