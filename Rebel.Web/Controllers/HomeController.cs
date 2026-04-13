using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rebel.Infrastructure.Data;
using Rebel.Web.Models;
using System.Diagnostics;

namespace Rebel.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;

        public HomeController(ILogger<HomeController> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var today = DateTime.UtcNow.Date;

            var allUpcomingEvents = _context.Events
                .AsEnumerable()
                .Where(e => e.IsActive && e.Date.Date >= today)
                .OrderBy(e => e.Date)
                .ToList();

            ViewBag.FeaturedEvent = allUpcomingEvents.FirstOrDefault();
            ViewBag.Events = allUpcomingEvents.Take(3).ToList();

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }

        public async Task<IActionResult> Menu(string section = "food")
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsAvailable)
                .ToListAsync();

            ViewBag.Section = section;

            return View(products);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}