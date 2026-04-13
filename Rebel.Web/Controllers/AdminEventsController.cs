using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rebel.Domain.Entities;
using Rebel.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Rendering;
using Rebel.Domain.Enums;



namespace Rebel.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminEventsController : Controller
    {
        private readonly AppDbContext _context;

        public AdminEventsController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var events = await _context.Events
                .OrderByDescending(e => e.Date)
                .ToListAsync();

            return View(events);
        }
        // GET
        public IActionResult Create()
        {
            LoadEventTypes();
            return View();
        }

        // POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Event model)
        {
            if (!ModelState.IsValid)
            {
                LoadEventTypes();
                return View(model);
            }

            model.Id = Guid.NewGuid();
            model.Date = DateTime.SpecifyKind(model.Date, DateTimeKind.Utc);

            _context.Events.Add(model);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "AdminEvents");
        }

        // GET
        public async Task<IActionResult> Edit(Guid id)
        {
            var ev = await _context.Events.FindAsync(id);

            if (ev == null)
                return NotFound();

            LoadEventTypes();
            return View(ev);
        }

        // POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, Event model)
        {
            if (id != model.Id)
                return NotFound();

            if (!ModelState.IsValid)
            {
                LoadEventTypes();
                return View(model);
            }

            model.Date = DateTime.SpecifyKind(model.Date, DateTimeKind.Utc);

            _context.Events.Update(model);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "AdminEvents");
        }
        // GET
        public async Task<IActionResult> Delete(Guid id)
        {
            var ev = await _context.Events.FindAsync(id);

            if (ev == null)
                return NotFound();

            return View(ev);
        }

        // POST
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var ev = await _context.Events.FindAsync(id);

            if (ev != null)
            {
                _context.Events.Remove(ev);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }


        private void LoadEventTypes()
        {
            ViewBag.EventTypes = Enum.GetValues(typeof(EventType))
                .Cast<EventType>()
                .Select(e => new SelectListItem
                {
                    Value = ((int)e).ToString(),
                    Text = e.ToString()
                })
                .ToList();
        }


    }
}