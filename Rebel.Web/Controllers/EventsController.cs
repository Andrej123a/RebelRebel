using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rebel.Infrastructure.Data;

namespace Rebel.Web.Controllers
{
    public class EventsController : Controller
    {
        private readonly AppDbContext _context;

        public EventsController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var today = DateTime.UtcNow.Date;

            var events = await _context.Events
                .Where(e => e.IsActive && e.Date >= today)
                .OrderBy(e => e.Date)
                .ToListAsync();

            return View(events);
        }

        public async Task<IActionResult> Details(Guid id)
        {
            var ev = await _context.Events
                .FirstOrDefaultAsync(e => e.Id == id && e.IsActive);

            if (ev == null)
                return NotFound();

            return View(ev);
        }
    }
}