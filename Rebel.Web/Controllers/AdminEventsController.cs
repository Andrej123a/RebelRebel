using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Rebel.Domain.Entities;
using Rebel.Domain.Enums;
using Rebel.Infrastructure.Data;

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

        // INDEX
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var events = await _context.Events
                .AsNoTracking()
                .OrderByDescending(e => e.Date)
                .ThenBy(e => e.StartTime)
                .ToListAsync();

            return View(events);
        }

        // CREATE GET
        [HttpGet]
        public IActionResult Create()
        {
            LoadEventTypes();

            var model = new Event
            {
                Date = DateTime.UtcNow.Date,
                IsActive = true
            };

            return View(model);
        }

        // CREATE POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Event model)
        {
            ValidateEventTimes(model);

            if (!ModelState.IsValid)
            {
                LoadEventTypes();
                return View(model);
            }

            model.Id = Guid.NewGuid();
            model.Title = model.Title.Trim();
            model.Description = model.Description.Trim();

            model.ImageUrl = string.IsNullOrWhiteSpace(model.ImageUrl)
                ? null
                : model.ImageUrl.Trim();

            model.Date = DateTime.SpecifyKind(
                model.Date.Date,
                DateTimeKind.Utc
            );

            _context.Events.Add(model);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Event created successfully.";

            return RedirectToAction(nameof(Index));
        }

        // EDIT GET
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var ev = await _context.Events
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == id);

            if (ev == null)
            {
                return NotFound();
            }

            LoadEventTypes();

            return View(ev);
        }

        // EDIT POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, Event model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            ValidateEventTimes(model);

            if (!ModelState.IsValid)
            {
                LoadEventTypes();
                return View(model);
            }

            var existingEvent = await _context.Events.FindAsync(id);

            if (existingEvent == null)
            {
                return NotFound();
            }

            existingEvent.Title = model.Title.Trim();
            existingEvent.Description = model.Description.Trim();

            existingEvent.Date = DateTime.SpecifyKind(
                model.Date.Date,
                DateTimeKind.Utc
            );

            existingEvent.StartTime = model.StartTime;
            existingEvent.EndTime = model.EndTime;
            existingEvent.EventType = model.EventType;
            existingEvent.IsActive = model.IsActive;

            existingEvent.ImageUrl = string.IsNullOrWhiteSpace(model.ImageUrl)
                ? null
                : model.ImageUrl.Trim();

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Event updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        // DELETE GET
        [HttpGet]
        public async Task<IActionResult> Delete(Guid id)
        {
            var ev = await _context.Events
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == id);

            if (ev == null)
            {
                return NotFound();
            }

            return View(ev);
        }

        // DELETE POST
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var ev = await _context.Events.FindAsync(id);

            if (ev == null)
            {
                return NotFound();
            }

            _context.Events.Remove(ev);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Event deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        private void ValidateEventTimes(Event model)
        {
            if (!model.StartTime.HasValue || !model.EndTime.HasValue)
            {
                return;
            }

            // Пример 22:00–02:00 е дозволен:
            // завршува следниот ден.
            if (model.StartTime.Value == model.EndTime.Value)
            {
                ModelState.AddModelError(
                    nameof(model.EndTime),
                    "Start time and end time cannot be the same."
                );
            }
        }

        private void LoadEventTypes()
        {
            ViewBag.EventTypes = Enum
                .GetValues<EventType>()
                .Select(eventType => new SelectListItem
                {
                    Value = ((int)eventType).ToString(),
                    Text = FormatEventType(eventType)
                })
                .ToList();
        }

        private static string FormatEventType(EventType eventType)
        {
            return eventType switch
            {
                EventType.DJNight => "DJ Night",
                EventType.BeerTasting => "Beer Tasting",
                EventType.LiveMusic => "Live Music",
                EventType.SpecialEvent => "Special Event",
                EventType.AcousticNight => "Acoustic Night",
                _ => eventType.ToString()
            };
        }
    }
}