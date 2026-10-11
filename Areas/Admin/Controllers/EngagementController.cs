using HumayQasimli.Data;
using HumayQasimli.Models.Entities;
using HumayQasimli.Models.ViewModels;
using HumayQasimli.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HumayQasimli.Areas.Admin.Controllers
{
    public class EngagementController : AdminBaseController
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationService _notificationService;

        public EngagementController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, INotificationService notificationService)
        {
            _db = db;
            _userManager = userManager;
            _notificationService = notificationService;
        }

        public async Task<IActionResult> Likes(int page = 1)
        {
            var query = _db.Likes.Include(l => l.User).Include(l => l.Vlog).OrderByDescending(l => l.CreatedAt);
            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * DefaultPageSize).Take(DefaultPageSize).ToListAsync();
            return View(new PagedResult<Like> { Items = items, PageNumber = page, PageSize = DefaultPageSize, TotalCount = total });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteLike(int id)
        {
            var like = await _db.Likes.FindAsync(id);
            if (like == null) return NotFound();
            _db.Likes.Remove(like);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Likes));
        }

        public async Task<IActionResult> Notifications()
        {
            var userId = _userManager.GetUserId(User)!;
            var all = await _db.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(100)
                .ToListAsync();

            var vm = new NotificationsPageViewModel
            {
                Unread = all.Where(n => !n.IsRead).ToList(),
                Read = all.Where(n => n.IsRead).ToList()
            };

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> MarkNotificationRead(int id)
        {
            var userId = _userManager.GetUserId(User)!;
            await _notificationService.MarkAsReadAsync(id, userId);
            return RedirectToAction(nameof(Notifications));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteNotification(int id)
        {
            var userId = _userManager.GetUserId(User)!;
            await _notificationService.DeleteAsync(id, userId);
            return RedirectToAction(nameof(Notifications));
        }
    }
}