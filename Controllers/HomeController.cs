using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SUPK.Models;
using System.Diagnostics;

namespace SUPK.Controllers
{
    public class HomeController : Controller
    {
        private readonly CaffeBarDbContext _context;

        public HomeController(CaffeBarDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string searchString)
        {
            var racuniQuery = _context.Racuns
                .Include(r => r.Konobar)
                .Include(r => r.Stol)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                racuniQuery = racuniQuery.Where(r => 
                    (r.Konobar != null && r.Konobar.Ime.Contains(searchString)) ||
                    (r.Stol != null && r.Stol.BrojStola.ToString().Contains(searchString)) ||
                    r.RacunId.ToString().Contains(searchString));
            }

            ViewData["CurrentFilter"] = searchString;
            return View(await racuniQuery.OrderByDescending(r => r.VrijemeOtvaranja).ToListAsync());
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
