using HumayQasimli.Data;
using HumayQasimli.Models.Entities;
using HumayQasimli.Models.ViewModels;
using HumayQasimli.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HumayQasimli.Areas.Admin.Controllers
{
    public class VlogsController : AdminBaseController
    {
        private readonly ApplicationDbContext _db;
        private readonly IFileUploadService _fileUploadService;

        public VlogsController(ApplicationDbContext db, IFileUploadService fileUploadService)
        {
            _db = db;
            _fileUploadService = fileUploadService;
        }

        public async Task<IActionResult> Index(string? search, int page = 1)
        {
            var query = _db.Vlogs.Include(v => v.Category).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(v => v.Title.Contains(search));

            var total = await query.CountAsync();
            var items = await query.OrderByDescending(v => v.CreatedAt)
                .Skip((page - 1) * DefaultPageSize).Take(DefaultPageSize).ToListAsync();

            return View(new PagedResult<Vlog>
            {
                Items = items,
                PageNumber = page,
                PageSize = DefaultPageSize,
                TotalCount = total,
                SearchTerm = search
            });
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await PopulateCategories();
            PopulateVideoTypes();
            return View(new VlogFormViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Create(VlogFormViewModel model)
        {
            ValidateVideoFields(model, isEdit: false, existingVideoUrl: null);

            if (!ModelState.IsValid)
            {
                await PopulateCategories(model.CategoryId);
                PopulateVideoTypes(model.VideoSourceType);
                return View(model);
            }

            var vlog = new Vlog
            {
                Title = model.Title,
                Slug = Slugify(model.Title) + "-" + Guid.NewGuid().ToString("N")[..6],
                Description = model.Description,
                VideoSourceType = model.VideoSourceType,
                CategoryId = model.CategoryId,
                IsFeatured = model.IsFeatured,
                IsPublished = model.IsPublished,
                PublishDate = model.PublishDate ?? DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            if (model.CoverImage != null)
            {
                var (success, path, error) = await _fileUploadService.SaveFileAsync(model.CoverImage, "vlogs");
                if (!success)
                {
                    ModelState.AddModelError(nameof(model.CoverImage), error ?? "Sekil yuklenmedi.");
                    await PopulateCategories(model.CategoryId);
                    PopulateVideoTypes(model.VideoSourceType);
                    return View(model);
                }
                vlog.CoverImageUrl = path;
            }

            if (model.VideoSourceType == VideoSourceType.ExternalLink)
            {
                vlog.VideoUrl = model.VideoUrl!.Trim();
            }
            else
            {
                var (success, path, error) = await _fileUploadService.SaveFileAsync(model.VideoFile!, "vlogs/videos", allowVideo: true);
                if (!success)
                {
                    ModelState.AddModelError(nameof(model.VideoFile), error ?? "Video yuklenmedi.");
                    await PopulateCategories(model.CategoryId);
                    PopulateVideoTypes(model.VideoSourceType);
                    return View(model);
                }
                vlog.VideoUrl = path!;
            }

            _db.Vlogs.Add(vlog);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Vlog elave edildi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var vlog = await _db.Vlogs.FindAsync(id);
            if (vlog == null) return NotFound();

            await PopulateCategories(vlog.CategoryId);
            PopulateVideoTypes(vlog.VideoSourceType);

            return View(new VlogFormViewModel
            {
                Id = vlog.Id,
                Title = vlog.Title,
                Description = vlog.Description,
                VideoUrl = vlog.VideoSourceType == VideoSourceType.ExternalLink ? vlog.VideoUrl : null,
                VideoSourceType = vlog.VideoSourceType,
                CategoryId = vlog.CategoryId,
                IsFeatured = vlog.IsFeatured,
                IsPublished = vlog.IsPublished,
                PublishDate = vlog.PublishDate,
                CurrentCoverImageUrl = vlog.CoverImageUrl,
                CurrentVideoUrl = vlog.VideoUrl
            });
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, VlogFormViewModel model)
        {
            if (id != model.Id) return NotFound();

            var vlog = await _db.Vlogs.FindAsync(id);
            if (vlog == null) return NotFound();

            ValidateVideoFields(model, isEdit: true, existingVideoUrl: vlog.VideoUrl);

            if (!ModelState.IsValid)
            {
                model.CurrentCoverImageUrl = vlog.CoverImageUrl;
                model.CurrentVideoUrl = vlog.VideoUrl;
                await PopulateCategories(model.CategoryId);
                PopulateVideoTypes(model.VideoSourceType);
                return View(model);
            }

            vlog.Title = model.Title;
            vlog.Description = model.Description;
            vlog.CategoryId = model.CategoryId;
            vlog.IsFeatured = model.IsFeatured;
            vlog.IsPublished = model.IsPublished;
            vlog.PublishDate = model.PublishDate ?? vlog.PublishDate;
            vlog.UpdatedAt = DateTime.UtcNow;

            if (model.RemoveCoverImage)
            {
                _fileUploadService.DeleteFile(vlog.CoverImageUrl);
                vlog.CoverImageUrl = null;
            }
            else if (model.CoverImage != null)
            {
                var (success, path, error) = await _fileUploadService.SaveFileAsync(model.CoverImage, "vlogs");
                if (!success)
                {
                    ModelState.AddModelError(nameof(model.CoverImage), error ?? "Sekil yuklenmedi.");
                    model.CurrentCoverImageUrl = vlog.CoverImageUrl;
                    model.CurrentVideoUrl = vlog.VideoUrl;
                    await PopulateCategories(model.CategoryId);
                    PopulateVideoTypes(model.VideoSourceType);
                    return View(model);
                }

                _fileUploadService.DeleteFile(vlog.CoverImageUrl);
                vlog.CoverImageUrl = path;
            }

            var wasUploadedVideo = vlog.VideoSourceType == VideoSourceType.UploadedVideo;

            if (model.VideoSourceType == VideoSourceType.ExternalLink)
            {
                if (wasUploadedVideo) _fileUploadService.DeleteFile(vlog.VideoUrl);
                vlog.VideoUrl = model.VideoUrl!.Trim();
                vlog.VideoSourceType = VideoSourceType.ExternalLink;
            }
            else if (model.VideoFile != null)
            {
                var (success, path, error) = await _fileUploadService.SaveFileAsync(model.VideoFile, "vlogs/videos", allowVideo: true);
                if (!success)
                {
                    ModelState.AddModelError(nameof(model.VideoFile), error ?? "Video yuklenmedi.");
                    model.CurrentCoverImageUrl = vlog.CoverImageUrl;
                    model.CurrentVideoUrl = vlog.VideoUrl;
                    await PopulateCategories(model.CategoryId);
                    PopulateVideoTypes(model.VideoSourceType);
                    return View(model);
                }

                if (wasUploadedVideo) _fileUploadService.DeleteFile(vlog.VideoUrl);
                vlog.VideoUrl = path!;
                vlog.VideoSourceType = VideoSourceType.UploadedVideo;
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = "Vlog yenilendi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var vlog = await _db.Vlogs.FindAsync(id);
            if (vlog == null) return NotFound();

            _fileUploadService.DeleteFile(vlog.CoverImageUrl);
            if (vlog.VideoSourceType == VideoSourceType.UploadedVideo)
                _fileUploadService.DeleteFile(vlog.VideoUrl);

            _db.Vlogs.Remove(vlog);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Vlog silindi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> TogglePublish(int id)
        {
            var vlog = await _db.Vlogs.FindAsync(id);
            if (vlog == null) return NotFound();

            vlog.IsPublished = !vlog.IsPublished;
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private void ValidateVideoFields(VlogFormViewModel model, bool isEdit, string? existingVideoUrl)
        {
            if (model.VideoSourceType == VideoSourceType.ExternalLink)
            {
                if (string.IsNullOrWhiteSpace(model.VideoUrl))
                {
                    ModelState.AddModelError(nameof(model.VideoUrl), "Xarici keçid üçün link daxil edin.");
                }
                else if (!Uri.TryCreate(model.VideoUrl, UriKind.Absolute, out _))
                {
                    ModelState.AddModelError(nameof(model.VideoUrl), "Düzgün bir link daxil edin.");
                }
            }
            else
            {
                var hasExistingUploadedVideo = isEdit && !string.IsNullOrEmpty(existingVideoUrl);
                if (model.VideoFile == null && !hasExistingUploadedVideo)
                {
                    ModelState.AddModelError(nameof(model.VideoFile), "Video faylı seçin.");
                }
            }
        }

        private async Task PopulateCategories(int? selected = null)
        {
            ViewBag.Categories = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                await _db.Categories.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", selected);
        }

        private void PopulateVideoTypes(VideoSourceType selected = VideoSourceType.ExternalLink)
        {
            var options = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>
            {
                new("Xarici keçid (Instagram / LinkedIn / TikTok / YouTube - link kimi açılır)", VideoSourceType.ExternalLink.ToString(), selected == VideoSourceType.ExternalLink),
                new("Video faylı yüklə (kompüter/telefon qalereyasından - saytda birbaşa oynayır)", VideoSourceType.UploadedVideo.ToString(), selected == VideoSourceType.UploadedVideo)
            };
            ViewBag.VideoTypes = options;
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

    public class VlogFormViewModel
    {
        public int Id { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.StringLength(250)]
        public string Title { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required]
        public string Description { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required]
        public VideoSourceType VideoSourceType { get; set; } = VideoSourceType.ExternalLink;

        public string? VideoUrl { get; set; }

        public IFormFile? VideoFile { get; set; }
        public string? CurrentVideoUrl { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        public int CategoryId { get; set; }

        public bool IsFeatured { get; set; }
        public bool IsPublished { get; set; }
        public DateTime? PublishDate { get; set; }

        public IFormFile? CoverImage { get; set; }
        public bool RemoveCoverImage { get; set; }
        public string? CurrentCoverImageUrl { get; set; }
    }
}