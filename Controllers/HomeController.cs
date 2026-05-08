using Microsoft.AspNetCore.Mvc;
using SUPK.Models;
using System.Diagnostics;

namespace SUPK.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            var dbContext = new CaffeBarDbContext();
            var narudzbas = dbContext.Narudzbas.ToList();
            return View(narudzbas);
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
