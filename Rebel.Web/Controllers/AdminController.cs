using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rebel.Infrastructure.Data;
using Rebel.Web.Models;

namespace Rebel.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var model = new DashboardViewModel
            {
                CategoriesCount = await _context.Categories.CountAsync(),
                ProductsCount = await _context.Products.CountAsync(),
                EventsCount = await _context.Events.CountAsync(),
                UpcomingEvents = await _context.Events
                    .Where(e => e.Date >= DateTime.UtcNow)
                    .OrderBy(e => e.Date)
                    .Take(3)
                    .ToListAsync()
            };

            return View(model);
        }
    }
}