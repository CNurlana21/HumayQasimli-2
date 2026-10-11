using HumayQasimli.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HumayQasimli.Controllers
{
    public class MentorMeetingController : Controller
    {
        private readonly ApplicationDbContext _db;

        public MentorMeetingController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            var meetings = await _db.MentorMeetings
                .Where(m => m.IsActive)
                .OrderBy(m => m.DisplayOrder)
                .ToListAsync();

            return View(meetings);
        }
    }
}
