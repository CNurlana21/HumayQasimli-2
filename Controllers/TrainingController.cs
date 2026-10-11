using HumayQasimli.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HumayQasimli.Controllers
{
    public class TrainingController : Controller
    {
        private readonly ApplicationDbContext _db;

        public TrainingController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            var trainings = await _db.Trainings
                .Where(t => t.IsActive)
                .OrderByDescending(t => t.StartDate)
                .ToListAsync();

            return View(trainings);
        }

        public async Task<IActionResult> Details(int id)
        {
            var training = await _db.Trainings.FirstOrDefaultAsync(t => t.Id == id && t.IsActive);
            if (training == null) return NotFound();
            return View(training);
        }
    }
}
