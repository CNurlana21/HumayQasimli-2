using HumayQasimli.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HumayQasimli.Controllers
{
    public class CertificateController : Controller
    {
        private readonly ApplicationDbContext _db;

        public CertificateController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            var certificates = await _db.Certificates
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

            return View(certificates);
        }
    }
}
