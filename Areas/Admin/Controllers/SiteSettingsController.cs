using HumayQasimli.Data;
using HumayQasimli.Models.Entities;
using HumayQasimli.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HumayQasimli.Areas.Admin.Controllers
{
    /// <summary>
    /// Manages the single-row SiteSetting entity. Split into separate pages
    /// (Profile / About / SocialLinks / General) to match the requested Admin
    /// Panel menu structure, while keeping a single underlying table.
    /// </summary>
    public class SiteSettingsController : AdminBaseController
    {
        private readonly ApplicationDbContext _db;
        private readonly IFileUploadService _fileUploadService;

        public SiteSettingsController(ApplicationDbContext db, IFileUploadService fileUploadService)
        {
            _db = db;
            _fileUploadService = fileUploadService;
        }

        private async Task<SiteSetting> GetOrCreateSettingsAsync()
        {
            var settings = await _db.SiteSettings.FirstOrDefaultAsync();
            if (settings == null)
            {
                settings = new SiteSetting();
                _db.SiteSettings.Add(settings);
                await _db.SaveChangesAsync();
            }
            return settings;
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            return View(await GetOrCreateSettingsAsync());
        }

        [HttpPost]
        public async Task<IActionResult> Profile(SiteSetting model, IFormFile? profileImage, bool removeProfileImage)
        {
            var settings = await GetOrCreateSettingsAsync();

            settings.SiteName = model.SiteName;
            settings.HeroTitle = model.HeroTitle;
            settings.HeroSubtitle = model.HeroSubtitle;

            if (removeProfileImage)
            {
                _fileUploadService.DeleteFile(settings.ProfileImageUrl);
                settings.ProfileImageUrl = null;
            }
            else if (profileImage != null)
            {
                var (ok, path, err) = await _fileUploadService.SaveFileAsync(profileImage, "profile");
                if (!ok) { ModelState.AddModelError("", err!); return View(settings); }
                _fileUploadService.DeleteFile(settings.ProfileImageUrl);
                settings.ProfileImageUrl = path;
            }

            settings.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Profil melumatlari yenilendi.";
            return RedirectToAction(nameof(Profile));
        }

        [HttpGet]
        public async Task<IActionResult> About()
        {
            return View(await GetOrCreateSettingsAsync());
        }

        [HttpPost]
        public async Task<IActionResult> About(SiteSetting model)
        {
            var settings = await GetOrCreateSettingsAsync();
            settings.AboutTitle = model.AboutTitle;
            settings.AboutContent = model.AboutContent;
            settings.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Haqqimda bolmesi yenilendi.";
            return RedirectToAction(nameof(About));
        }

        [HttpGet]
        public async Task<IActionResult> SocialLinks()
        {
            return View(await GetOrCreateSettingsAsync());
        }

        [HttpPost]
        public async Task<IActionResult> SocialLinks(SiteSetting model)
        {
            var settings = await GetOrCreateSettingsAsync();
            settings.InstagramUrl = model.InstagramUrl;
            settings.LinkedInUrl = model.LinkedInUrl;
            settings.YoutubeUrl = model.YoutubeUrl;
            settings.WhatsAppNumber = model.WhatsAppNumber;
            settings.ContactEmail = model.ContactEmail;
            settings.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Elaqe linkleri yenilendi.";
            return RedirectToAction(nameof(SocialLinks));
        }

        [HttpGet]
        public async Task<IActionResult> General()
        {
            return View(await GetOrCreateSettingsAsync());
        }

        [HttpPost]
        public async Task<IActionResult> General(SiteSetting model)
        {
            var settings = await GetOrCreateSettingsAsync();
            settings.FooterText = model.FooterText;
            settings.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Sayt tenzimlemeleri yenilendi.";
            return RedirectToAction(nameof(General));
        }
    }
}
