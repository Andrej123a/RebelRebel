using Microsoft.AspNetCore.Mvc;
using Rebel.Domain.Entities;
using Rebel.Domain.Enums;
using Rebel.Infrastructure.Data;
using Rebel.Web.Models;

namespace Rebel.Web.Controllers
{
    public class ReservationsController : Controller
    {
        private readonly AppDbContext _context;

        private static readonly TimeZoneInfo SkopjeTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById("Europe/Skopje");

        public ReservationsController(AppDbContext context)
        {
            _context = context;
        }

        // CREATE GET
        [HttpGet]
        public IActionResult Create()
        {
            var nowInSkopje = GetCurrentSkopjeTime();

            PrepareForm(nowInSkopje);

            var model = new ReservationCreateViewModel
            {
                ReservationDate = nowInSkopje.Date,
                ReservationTime = new TimeSpan(20, 0, 0),
                NumberOfGuests = 2
            };

            return View(model);
        }

        // CREATE POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            ReservationCreateViewModel model
        )
        {
            var nowInSkopje = GetCurrentSkopjeTime();

            ValidateReservationDateTime(model, nowInSkopje);

            if (!ModelState.IsValid)
            {
                PrepareForm(nowInSkopje);
                return View(model);
            }

            var reservation = new Reservation
            {
                Id = Guid.NewGuid(),

                FullName = model.FullName.Trim(),
                Email = model.Email.Trim(),
                PhoneNumber = model.PhoneNumber.Trim(),

                ReservationDate = model.ReservationDate.Date,
                ReservationTime = model.ReservationTime,
                NumberOfGuests = model.NumberOfGuests,

                Note = string.IsNullOrWhiteSpace(model.Note)
                    ? null
                    : model.Note.Trim(),

                Status = ReservationStatus.Pending,
                CreatedAtUtc = DateTime.UtcNow
            };

            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();

            TempData["ReservationSubmitted"] = true;
            TempData["ReservationName"] = reservation.FullName;

            return RedirectToAction(nameof(Confirmation));
        }

        // CONFIRMATION
        [HttpGet]
        public IActionResult Confirmation()
        {
            if (TempData["ReservationSubmitted"] == null)
            {
                return RedirectToAction(nameof(Create));
            }

            ViewBag.ReservationName =
                TempData["ReservationName"]?.ToString();

            return View();
        }

        private static DateTime GetCurrentSkopjeTime()
        {
            return TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                SkopjeTimeZone
            );
        }

        private void ValidateReservationDateTime(
            ReservationCreateViewModel model,
            DateTime nowInSkopje
        )
        {
            if (model.ReservationDate == default)
            {
                return;
            }

            var requestedDateTime =
                model.ReservationDate.Date
                .Add(model.ReservationTime);

            if (requestedDateTime <= nowInSkopje)
            {
                ModelState.AddModelError(
                    nameof(model.ReservationDate),
                    "Please select a future date and time."
                );
            }
        }

        private void PrepareForm(DateTime nowInSkopje)
        {
            ViewBag.MinimumReservationDate =
                nowInSkopje.ToString("yyyy-MM-dd");
        }
    }
}