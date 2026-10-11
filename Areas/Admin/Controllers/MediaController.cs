using HumayQasimli.Data;
using HumayQasimli.Models.Entities;
using HumayQasimli.Models.ViewModels;
using HumayQasimli.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HumayQasimli.Areas.Admin.Controllers
{
    public class MediaController : AdminBaseController
    {
        private readonly ApplicationDbContext _db;
        private readonly IFileUploadService _fileUploadService;
        private readonly UserManager<ApplicationUser> _userManager;

        public MediaController(ApplicationDbContext db, IFileUploadService fileUploadService, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _fileUploadService = fileUploadService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string? search, int page = 1)
        {
            var query = _db.MediaItems.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search)) query = query.Where(m => m.FileName.Contains(search));

            var total = await query.CountAsync();
            var items = await query.OrderByDescending(m => m.UploadedAt)
                .Skip((page - 1) * DefaultPageSize).Take(DefaultPageSize).ToListAsync();

            return View(new PagedResult<Media> { Items = items, PageNumber = page, PageSize = DefaultPageSize, TotalCount = total, SearchTerm = search });
        }

        [HttpGet]
        public IActionResult Upload() => View();

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            var (ok, path, err) = await _fileUploadService.SaveFileAsync(file, "media", allowPdf: true);
            if (!ok)
            {
                ModelState.AddModelError("", err!);
                return View();
            }

            var userId = _userManager.GetUserId(User);

            _db.MediaItems.Add(new Media
            {
                FileName = file.FileName,
                FilePath = path!,
                FileType = file.ContentType,
                FileSize = file.Length,
                UploadedByUserId = userId,
                UploadedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            TempData["Success"] = "Fayl yuklendi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var media = await _db.MediaItems.FindAsync(id);
            if (media == null) return NotFound();

            _fileUploadService.DeleteFile(media.FilePath);
            _db.MediaItems.Remove(media);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Fayl silindi.";
            return RedirectToAction(nameof(Index));
        }
    }
}
