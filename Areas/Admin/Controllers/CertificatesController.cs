using HumayQasimli.Data;
using HumayQasimli.Models.Entities;
using HumayQasimli.Models.ViewModels;
using HumayQasimli.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HumayQasimli.Areas.Admin.Controllers
{
    public class CertificatesController : AdminBaseController
    {
        private readonly ApplicationDbContext _db;
        private readonly IFileUploadService _fileUploadService;
        public CertificatesController(ApplicationDbContext db, IFileUploadService fileUploadService) { _db = db; _fileUploadService = fileUploadService; }

        public async Task<IActionResult> Index(string? search, int page = 1)
        {
            var query = _db.Certificates.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search)) query = query.Where(c => c.Title.Contains(search));
            var total = await query.CountAsync();
            var items = await query.OrderBy(c => c.DisplayOrder).Skip((page - 1) * DefaultPageSize).Take(DefaultPageSize).ToListAsync();
            return View(new PagedResult<Certificate> { Items = items, PageNumber = page, PageSize = DefaultPageSize, TotalCount = total, SearchTerm = search });
        }

        [HttpGet] public IActionResult Create() => View(new Certificate());

        [HttpPost]
        public async Task<IActionResult> Create(Certificate model, IFormFile? image)
        {
            if (!ModelState.IsValid) return View(model);
            model.CreatedAt = DateTime.UtcNow;

            if (image != null)
            {
                var (ok, path, err) = await _fileUploadService.SaveFileAsync(image, "certificates", allowPdf: true);
                if (!ok) { ModelState.AddModelError("", err!); return View(model); }
                model.ImageUrl = path;
            }

            _db.Certificates.Add(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Sertifikat elave edildi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var item = await _db.Certificates.FindAsync(id);
            if (item == null) return NotFound();
            return View(item);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, Certificate model, IFormFile? image, bool removeImage)
        {
            if (id != model.Id) return NotFound();
            if (!ModelState.IsValid) return View(model);
            var item = await _db.Certificates.FindAsync(id);
            if (item == null) return NotFound();

            item.Title = model.Title; item.IssuedBy = model.IssuedBy; item.IssueDate = model.IssueDate;
            item.CredentialUrl = model.CredentialUrl; item.DisplayOrder = model.DisplayOrder;

            if (removeImage)
            {
                _fileUploadService.DeleteFile(item.ImageUrl);
                item.ImageUrl = null;
            }
            else if (image != null)
            {
                var (ok, path, err) = await _fileUploadService.SaveFileAsync(image, "certificates", allowPdf: true);
                if (!ok) { ModelState.AddModelError("", err!); return View(model); }
                _fileUploadService.DeleteFile(item.ImageUrl);
                item.ImageUrl = path;
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = "Sertifikat yenilendi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _db.Certificates.FindAsync(id);
            if (item == null) return NotFound();
            _fileUploadService.DeleteFile(item.ImageUrl);
            _db.Certificates.Remove(item);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Sertifikat silindi.";
            return RedirectToAction(nameof(Index));
        }
    }
}
