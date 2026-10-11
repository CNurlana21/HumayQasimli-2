using HumayQasimli.Data;
using HumayQasimli.Models.Entities;
using HumayQasimli.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HumayQasimli.Controllers
{
    public class ContactController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public ContactController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.SiteSettings = await _db.SiteSettings.FirstOrDefaultAsync();
            return View(new ContactFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ContactFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.SiteSettings = await _db.SiteSettings.FirstOrDefaultAsync();
                return View(model);
            }

            var userId = _userManager.GetUserId(User);

            _db.ContactMessages.Add(new ContactMessage
            {
                Name = model.Name,
                Email = model.Email,
                Subject = model.Subject,
                Message = model.Message,
                CreatedAt = DateTime.UtcNow,
                IsRead = false,
                UserId = userId
            });

            await _db.SaveChangesAsync();

            TempData["Success"] = "Mesajınız uğurla göndərildi. Təşşəkür edirik!";
            return RedirectToAction(nameof(Index));
        }
    }
}