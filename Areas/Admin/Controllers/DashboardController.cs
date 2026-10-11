using HumayQasimli.Data;
using HumayQasimli.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HumayQasimli.Areas.Admin.Controllers
{
    public class DashboardController : AdminBaseController
    {
        private readonly ApplicationDbContext _db;

        public DashboardController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            var vm = new AdminDashboardViewModel
            {
                TotalUsers = await _db.Users.CountAsync(),
                TotalVlogs = await _db.Vlogs.CountAsync(),
                PublishedVlogs = await _db.Vlogs.CountAsync(v => v.IsPublished),
                TotalPortfolioProjects = await _db.PortfolioProjects.CountAsync(),
                TotalComments = await _db.Comments.CountAsync(c => !c.IsDeleted),
                TotalLikes = await _db.Likes.CountAsync(),
                UnreadContactMessages = await _db.ContactMessages.CountAsync(c => !c.IsRead),
                TotalCertificates = await _db.Certificates.CountAsync(),
            };

            return View(vm);
        }
    }
}
