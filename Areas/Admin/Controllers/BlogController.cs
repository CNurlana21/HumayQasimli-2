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
    public class BlogController : AdminBaseController
    {
        private readonly ApplicationDbContext _db;
        private readonly IFileUploadService _fileUploadService;

        public BlogController(ApplicationDbContext db, IFileUploadService fileUploadService)
        {
            _db = db;
            _fileUploadService = fileUploadService;
        }

        public async Task<IActionResult> Index(string? search, int page = 1)
        {
            var query = _db.Blogs.Include(b => b.Category).AsQueryable();
            if (!string.IsNullOrWhiteSpace(search)) query = query.Where(b => b.Title.Contains(search));

            var total = await query.CountAsync();
            var items = await query.OrderByDescending(b => b.CreatedAt)
                .Skip((page - 1) * DefaultPageSize).Take(DefaultPageSize).ToListAsync();

            return View(new PagedResult<Blog> { Items = items, PageNumber = page, PageSize = DefaultPageSize, TotalCount = total, SearchTerm = search });
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await PopulateCategories();
            return View(new BlogFormViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Create(BlogFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateCategories(model.CategoryId);
                return View(model);
            }

            var blog = new Blog
            {
                Title = model.Title,
                Slug = Slugify(model.Title) + "-" + Guid.NewGuid().ToString("N")[..6],
                ShortDescription = model.ShortDescription,
                Content = model.Content,
                CategoryId = model.CategoryId,
                IsPublished = model.IsPublished,
                PublishDate = model.PublishDate ?? DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            if (model.CoverImage != null)
            {
                var (success, path, error) = await _fileUploadService.SaveFileAsync(model.CoverImage, "blog");
                if (!success)
                {
                    ModelState.AddModelError(nameof(model.CoverImage), error ?? "Sekil yuklenmedi.");
                    await PopulateCategories(model.CategoryId);
                    return View(model);
                }
                blog.CoverImageUrl = path;
            }

            _db.Blogs.Add(blog);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Blog elave edildi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var blog = await _db.Blogs.FindAsync(id);
            if (blog == null) return NotFound();

            await PopulateCategories(blog.CategoryId);

            return View(new BlogFormViewModel
            {
                Id = blog.Id,
                Title = blog.Title,
                ShortDescription = blog.ShortDescription,
                Content = blog.Content,
                CategoryId = blog.CategoryId,
                IsPublished = blog.IsPublished,
                PublishDate = blog.PublishDate,
                CurrentCoverImageUrl = blog.CoverImageUrl
            });
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, BlogFormViewModel model)
        {
            if (id != model.Id) return NotFound();

            var blog = await _db.Blogs.FindAsync(id);
            if (blog == null) return NotFound();

            if (!ModelState.IsValid)
            {
                model.CurrentCoverImageUrl = blog.CoverImageUrl;
                await PopulateCategories(model.CategoryId);
                return View(model);
            }

            blog.Title = model.Title;
            blog.ShortDescription = model.ShortDescription;
            blog.Content = model.Content;
            blog.CategoryId = model.CategoryId;
            blog.IsPublished = model.IsPublished;
            blog.PublishDate = model.PublishDate ?? blog.PublishDate;
            blog.UpdatedAt = DateTime.UtcNow;

            if (model.RemoveCoverImage)
            {
                _fileUploadService.DeleteFile(blog.CoverImageUrl);
                blog.CoverImageUrl = null;
            }
            else if (model.CoverImage != null)
            {
                var (success, path, error) = await _fileUploadService.SaveFileAsync(model.CoverImage, "blog");
                if (!success)
                {
                    ModelState.AddModelError(nameof(model.CoverImage), error ?? "Sekil yuklenmedi.");
                    model.CurrentCoverImageUrl = blog.CoverImageUrl;
                    await PopulateCategories(model.CategoryId);
                    return View(model);
                }

                _fileUploadService.DeleteFile(blog.CoverImageUrl);
                blog.CoverImageUrl = path;
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = "Blog yenilendi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var blog = await _db.Blogs.FindAsync(id);
            if (blog == null) return NotFound();

            _fileUploadService.DeleteFile(blog.CoverImageUrl);
            _db.Blogs.Remove(blog);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Blog silindi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> TogglePublish(int id)
        {
            var blog = await _db.Blogs.FindAsync(id);
            if (blog == null) return NotFound();

            blog.IsPublished = !blog.IsPublished;
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateCategories(int? selected = null)
        {
            ViewBag.Categories = new SelectList(await _db.Categories.OrderBy(c => c.Name).ToListAsync(), "Id", "Name", selected);
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

    public class BlogFormViewModel
    {
        public int Id { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.StringLength(250)]
        public string Title { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.StringLength(500)]
        public string ShortDescription { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required]
        public string Content { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required]
        public int CategoryId { get; set; }

        public bool IsPublished { get; set; }
        public DateTime? PublishDate { get; set; }

        public IFormFile? CoverImage { get; set; }
        public bool RemoveCoverImage { get; set; }
        public string? CurrentCoverImageUrl { get; set; }
    }
}
