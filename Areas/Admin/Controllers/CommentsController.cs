using HumayQasimli.Data;
using HumayQasimli.Models.Entities;
using HumayQasimli.Models.ViewModels;
using HumayQasimli.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HumayQasimli.Areas.Admin.Controllers
{
    public class CommentsController : AdminBaseController
    {
        private readonly ApplicationDbContext _db;
        private readonly INotificationService _notificationService;

        public CommentsController(ApplicationDbContext db, INotificationService notificationService)
        {
            _db = db;
            _notificationService = notificationService;
        }

        public async Task<IActionResult> Index(string? search, int page = 1)
        {
            var query = _db.Comments.Include(c => c.User).Include(c => c.Vlog).Where(c => !c.IsDeleted).AsQueryable();
            if (!string.IsNullOrWhiteSpace(search)) query = query.Where(c => c.Content.Contains(search));

            var total = await query.CountAsync();
            var items = await query.OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * DefaultPageSize).Take(DefaultPageSize).ToListAsync();

            return View(new PagedResult<Comment> { Items = items, PageNumber = page, PageSize = DefaultPageSize, TotalCount = total, SearchTerm = search });
        }

        public async Task<IActionResult> Details(int id)
        {
            var comment = await _db.Comments.Include(c => c.User).Include(c => c.Vlog).FirstOrDefaultAsync(c => c.Id == id);
            if (comment == null) return NotFound();
            return View(comment);
        }

        [HttpPost]
        public async Task<IActionResult> Reply(int id, string reply)
        {
            var comment = await _db.Comments.FirstOrDefaultAsync(c => c.Id == id);
            if (comment == null) return NotFound();

            comment.AdminReply = reply;
            comment.RepliedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            var vlog = await _db.Vlogs.FindAsync(comment.VlogId);
            await _notificationService.CreateAsync(comment.UserId, NotificationType.AdminReply,
                $"Şərhinizə admin cavab verdi: \"{reply}\"", vlog != null ? $"/Vlog/Details/{vlog.Id}" : null);

            TempData["Success"] = "Cavab göndərildi.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var comment = await _db.Comments.FindAsync(id);
            if (comment == null) return NotFound();

            comment.IsDeleted = true;
            await _db.SaveChangesAsync();

            TempData["Success"] = "Şərh silindi.";
            return RedirectToAction(nameof(Index));
        }
    }
}