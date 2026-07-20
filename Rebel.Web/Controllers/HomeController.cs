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

        private static readonly TimeZoneInfo SkopjeTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById("Europe/Skopje");

        public HomeController(
            ILogger<HomeController> logger,
            AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            CancellationToken cancellationToken)
        {
            var skopjeNow = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                SkopjeTimeZone
            );

            var startOfTodayInSkopje = DateTime.SpecifyKind(
                skopjeNow.Date,
                DateTimeKind.Unspecified
            );

            var startOfTodayUtc = TimeZoneInfo.ConvertTimeToUtc(
                startOfTodayInSkopje,
                SkopjeTimeZone
            );

            var upcomingEvents = await _context.Events
                .AsNoTracking()
                .Where(eventItem =>
                    eventItem.IsActive &&
                    eventItem.Date >= startOfTodayUtc
                )
                .OrderBy(eventItem => eventItem.Date)
                .ThenBy(eventItem => eventItem.StartTime)
                .Take(4)
                .ToListAsync(cancellationToken);

            var model = new HomeViewModel
            {
                FeaturedEvent = upcomingEvents.FirstOrDefault(),

                UpcomingEvents = upcomingEvents
                    .Skip(1)
                    .Take(3)
                    .ToList()
            };

            return View(model);
        }

        [HttpGet]
        public IActionResult Privacy()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Contact()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Menu(
            string section = "food",
            CancellationToken cancellationToken = default)
        {
            var products = await _context.Products
                .AsNoTracking()
                .Include(product => product.Category)
                .ToListAsync(cancellationToken);

            ViewBag.Section = section;

            return View(products);
        }

        [ResponseCache(
            Duration = 0,
            Location = ResponseCacheLocation.None,
            NoStore = true)]
        public IActionResult Error()
        {
            return View(
                new ErrorViewModel
                {
                    RequestId =
                        Activity.Current?.Id ??
                        HttpContext.TraceIdentifier
                }
            );
        }
    }
}
