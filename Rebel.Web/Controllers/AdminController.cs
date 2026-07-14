using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rebel.Domain.Enums;
using Rebel.Infrastructure.Data;
using Rebel.Web.Models;

namespace Rebel.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;

        private static readonly TimeZoneInfo SkopjeTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById("Europe/Skopje");

        public AdminController(AppDbContext context)
        {
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

            var model = new DashboardViewModel
            {
                CategoriesCount = await _context.Categories
                    .CountAsync(cancellationToken),

                ProductsCount = await _context.Products
                    .CountAsync(cancellationToken),

                EventsCount = await _context.Events
                    .CountAsync(cancellationToken),

                ReservationsCount = await _context.Reservations
                    .CountAsync(cancellationToken),

                PendingReservationsCount = await _context.Reservations
                    .CountAsync(
                        reservation =>
                            reservation.Status == ReservationStatus.Pending,
                        cancellationToken
                    ),

                UpcomingEvents = await _context.Events
                    .AsNoTracking()
                    .Where(eventItem =>
                        eventItem.IsActive &&
                        eventItem.Date >= startOfTodayUtc
                    )
                    .OrderBy(eventItem => eventItem.Date)
                    .ThenBy(eventItem => eventItem.StartTime)
                    .Take(3)
                    .ToListAsync(cancellationToken)
            };

            return View(model);
        }
    }
}