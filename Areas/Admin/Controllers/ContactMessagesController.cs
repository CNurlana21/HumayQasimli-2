using HumayQasimli.Data;
using HumayQasimli.Models.Entities;
using HumayQasimli.Models.ViewModels;
using HumayQasimli.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HumayQasimli.Areas.Admin.Controllers
{
    public class ContactMessagesController : AdminBaseController
    {
        private readonly ApplicationDbContext _db;
        private readonly INotificationService _notificationService;

        public ContactMessagesController(ApplicationDbContext db, INotificationService notificationService)
        {
            _db = db;
            _notificationService = notificationService;
        }

        public async Task<IActionResult> Index(string? search, int page = 1)
        {
            var query = _db.ContactMessages.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(m => m.Name.Contains(search) || m.Email.Contains(search) || m.Message.Contains(search));

            var total = await query.CountAsync();
            var items = await query.OrderByDescending(m => m.CreatedAt)
                .Skip((page - 1) * DefaultPageSize).Take(DefaultPageSize).ToListAsync();

            return View(new PagedResult<ContactMessage> { Items = items, PageNumber = page, PageSize = DefaultPageSize, TotalCount = total, SearchTerm = search });
        }

        public async Task<IActionResult> Details(int id)
        {
            var message = await _db.ContactMessages.FindAsync(id);
            if (message == null) return NotFound();

            if (!message.IsRead)
            {
                message.IsRead = true;
                await _db.SaveChangesAsync();
            }

            return View(message);
        }

        [HttpPost]
        public async Task<IActionResult> Reply(int id, string reply)
        {
            var message = await _db.ContactMessages.FindAsync(id);
            if (message == null) return NotFound();

            message.AdminReply = reply;
            message.RepliedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            if (!string.IsNullOrEmpty(message.UserId))
            {
                var combined = $"Sizin mesajınız: \"{Truncate(message.Message, 120)}\" — Admin cavabı: \"{reply}\"";
                await _notificationService.CreateAsync(message.UserId, NotificationType.AdminReply, combined, null);
            }

            TempData["Success"] = "Cavab gondərildi.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleRead(int id)
        {
            var message = await _db.ContactMessages.FindAsync(id);
            if (message == null) return NotFound();
            message.IsRead = !message.IsRead;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var message = await _db.ContactMessages.FindAsync(id);
            if (message == null) return NotFound();
            _db.ContactMessages.Remove(message);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Mesaj silindi.";
            return RedirectToAction(nameof(Index));
        }

        private static string Truncate(string text, int maxLength)
        {
            return text.Length <= maxLength ? text : text[..maxLength] + "…";
        }
    }
}