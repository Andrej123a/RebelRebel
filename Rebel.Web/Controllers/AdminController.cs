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

            var today = skopjeNow.Date;
            var tomorrow = today.AddDays(1);

            var tonightReservations = await _context.Reservations
                .AsNoTracking()
                .Where(reservation =>
                    reservation.ReservationDate >= today &&
                    reservation.ReservationDate < tomorrow &&
                    reservation.Status != ReservationStatus.Rejected)
                .OrderBy(reservation => reservation.ReservationTime)
                .ToListAsync(cancellationToken);

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

                TonightReservationsCount =
                    tonightReservations.Count,

                TonightGuestsCount =
                    tonightReservations.Sum(reservation =>
                        reservation.NumberOfGuests),

                TonightPendingReservationsCount =
                    tonightReservations.Count(reservation =>
                        reservation.Status == ReservationStatus.Pending),

                TonightUnassignedTablesCount =
                    tonightReservations.Count(reservation =>
                        reservation.Status != ReservationStatus.Pending &&
                        reservation.Status != ReservationStatus.NoShow &&
                        string.IsNullOrWhiteSpace(
                            reservation.TableLabel)),

                UnavailableProductsCount = await _context.Products
                    .CountAsync(
                        product => !product.IsAvailable,
                        cancellationToken),

                LatestPendingReservations = await _context.Reservations
                    .AsNoTracking()
                    .Where(reservation =>
                        reservation.Status == ReservationStatus.Pending)
                    .OrderBy(reservation =>
                        reservation.ReservationDate)
                    .ThenBy(reservation =>
                        reservation.ReservationTime)
                    .Take(5)
                    .ToListAsync(cancellationToken),

                UnavailableProducts = await _context.Products
                    .AsNoTracking()
                    .Include(product => product.Category)
                    .Where(product => !product.IsAvailable)
                    .OrderBy(product => product.Category!.Name)
                    .ThenBy(product => product.Name)
                    .Take(5)
                    .ToListAsync(cancellationToken),

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
