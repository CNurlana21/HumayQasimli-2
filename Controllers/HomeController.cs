using System.Diagnostics;
using HumayQasimli.Data;
using HumayQasimli.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HumayQasimli.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApplicationDbContext db, ILogger<HomeController> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var settings = await _db.SiteSettings.FirstOrDefaultAsync();

            var featuredVlogs = await _db.Vlogs
                .Where(v => v.IsPublished && v.IsFeatured)
                .OrderByDescending(v => v.PublishDate)
                .Take(3)
                .Include(v => v.Category)
                .ToListAsync();

            var featuredPortfolio = await _db.PortfolioProjects
                .Where(p => p.IsFeatured)
                .OrderByDescending(p => p.CreatedAt)
                .Take(3)
                .ToListAsync();

            ViewBag.SiteSettings = settings;
            ViewBag.FeaturedVlogs = featuredVlogs;
            ViewBag.FeaturedPortfolio = featuredPortfolio;

            return View();
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            return View(new ErrorViewModel { RequestId = requestId });
        }
    }
}
