using HumayQasimli.Data;
using HumayQasimli.Models.Entities;
using HumayQasimli.Models.ViewModels;
using HumayQasimli.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HumayQasimli.Areas.Admin.Controllers
{
    public class MentorMeetingsController : AdminBaseController
    {
        private readonly ApplicationDbContext _db;
        private readonly IFileUploadService _fileUploadService;

        public MentorMeetingsController(ApplicationDbContext db, IFileUploadService fileUploadService)
        {
            _db = db;
            _fileUploadService = fileUploadService;
        }

        public async Task<IActionResult> Index(string? search, int page = 1)
        {
            var query = _db.MentorMeetings.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search)) query = query.Where(m => m.Title.Contains(search));
            var total = await query.CountAsync();
            var items = await query.OrderBy(m => m.DisplayOrder).Skip((page - 1) * DefaultPageSize).Take(DefaultPageSize).ToListAsync();
            return View(new PagedResult<MentorMeeting> { Items = items, PageNumber = page, PageSize = DefaultPageSize, TotalCount = total, SearchTerm = search });
        }

        [HttpGet] public IActionResult Create() => View(new MentorMeeting());

        [HttpPost]
        public async Task<IActionResult> Create(MentorMeeting model, IFormFile? image)
        {
            if (!ModelState.IsValid) return View(model);
            if (model.CreatedAt == default) model.CreatedAt = DateTime.UtcNow;

            if (image != null)
            {
                var (ok, path, err) = await _fileUploadService.SaveFileAsync(image, "mentor-meetings");
                if (!ok) { ModelState.AddModelError("", err!); return View(model); }
                model.ImageUrl = path;
            }

            _db.MentorMeetings.Add(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Görüş əlavə edildi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var item = await _db.MentorMeetings.FindAsync(id);
            if (item == null) return NotFound();
            return View(item);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, MentorMeeting model, IFormFile? image, bool removeImage)
        {
            if (id != model.Id) return NotFound();
            if (!ModelState.IsValid) return View(model);
            var item = await _db.MentorMeetings.FindAsync(id);
            if (item == null) return NotFound();

            item.Title = model.Title; item.Description = model.Description; item.CreatedAt = model.CreatedAt;
            item.BookingUrl = model.BookingUrl; item.IsActive = model.IsActive; item.DisplayOrder = model.DisplayOrder;

            if (removeImage)
            {
                _fileUploadService.DeleteFile(item.ImageUrl);
                item.ImageUrl = null;
            }
            else if (image != null)
            {
                var (ok, path, err) = await _fileUploadService.SaveFileAsync(image, "mentor-meetings");
                if (!ok) { ModelState.AddModelError("", err!); return View(model); }
                _fileUploadService.DeleteFile(item.ImageUrl);
                item.ImageUrl = path;
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = "Görüş yenilendi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _db.MentorMeetings.FindAsync(id);
            if (item == null) return NotFound();
            _fileUploadService.DeleteFile(item.ImageUrl);
            _db.MentorMeetings.Remove(item);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Görüş silindi.";
            return RedirectToAction(nameof(Index));
        }
    }
}