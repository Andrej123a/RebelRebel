using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Rebel.Domain.Entities;
using Rebel.Domain.Enums;
using Rebel.Infrastructure.Data;
using Rebel.Web.Hubs;
using Rebel.Web.Models;
using Rebel.Web.Services;

namespace Rebel.Web.Controllers
{
    public class ReservationsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<NotificationHub> _notificationHub;

        private static readonly TimeZoneInfo SkopjeTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById("Europe/Skopje");

        public ReservationsController(
            AppDbContext context,
            IHubContext<NotificationHub> notificationHub)
        {
            _context = context;
            _notificationHub = notificationHub;
        }

        // CREATE GET
        [HttpGet]
        public async Task<IActionResult> Create(Guid? eventId)
        {
            var nowInSkopje = GetCurrentSkopjeTime();

            PrepareForm(nowInSkopje);

            var model = new ReservationCreateViewModel
            {
                ReservationDate = nowInSkopje.Date,
                ReservationTime = ReservationPolicy.FirstOnlineSlot,
                NumberOfGuests = 2
            };

            // Ако резервацијата доаѓа од конкретен event
            if (eventId.HasValue)
            {
                var selectedEvent = await _context.Events
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e =>
                        e.Id == eventId.Value &&
                        e.IsActive
                    );

                if (selectedEvent == null)
                {
                    return NotFound();
                }

                model.EventId = selectedEvent.Id;
                model.EventTitle = selectedEvent.Title;
                model.ReservationDate = selectedEvent.Date.Date;

                if (selectedEvent.StartTime.HasValue &&
                    ReservationPolicy.IsOnlineSlot(
                        selectedEvent.StartTime.Value))
                {
                    model.ReservationTime =
                        selectedEvent.StartTime.Value;
                }
            }

            return View(model);
        }

        // CREATE POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            ReservationCreateViewModel model)
        {
            var nowInSkopje = GetCurrentSkopjeTime();

            // Повторна проверка на event-от.
            // Не се потпираме само на EventId испратено од формата.
            if (model.EventId.HasValue)
            {
                var selectedEvent = await _context.Events
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e =>
                        e.Id == model.EventId.Value &&
                        e.IsActive
                    );

                if (selectedEvent == null)
                {
                    ModelState.AddModelError(
                        nameof(model.EventId),
                        "The selected event is no longer available."
                    );
                }
                else
                {
                    model.EventTitle = selectedEvent.Title;

                    // Резервацијата мора да остане на датумот
                    // на избраниот event.
                    model.ReservationDate =
                        selectedEvent.Date.Date;
                }
            }

            await ValidateReservationDateTime(
                model,
                nowInSkopje);

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

                EventId = model.EventId,

                Status = ReservationStatus.Pending,
                CreatedAtUtc = DateTime.UtcNow
            };

            var reservationDate =
                reservation.ReservationDate.ToString(
                    "dd MMM yyyy"
                );

            var reservationTime =
                reservation.ReservationTime.ToString(
                    @"hh\:mm"
                );

            var eventText =
                string.IsNullOrWhiteSpace(model.EventTitle)
                    ? string.Empty
                    : $" for {model.EventTitle}";

            var notification = new Notification
            {
                Title = "New table reservation",

                Message =
                    $"{reservation.FullName} requested a table for " +
                    $"{reservation.NumberOfGuests} guests on " +
                    $"{reservationDate} at {reservationTime}{eventText}.",

                Link = "/AdminReservations",

                IsRead = false,
                CreatedAt = DateTime.UtcNow,

                ReservationId = reservation.Id
            };

            _context.Reservations.Add(reservation);
            _context.Notifications.Add(notification);

            await _context.SaveChangesAsync();

            await _notificationHub.Clients.All.SendAsync(
                "ReceiveNotification",
                new
                {
                    id = notification.Id,
                    title = notification.Title,
                    message = notification.Message,
                    link = notification.Link,
                    createdAt = notification.CreatedAt
                },
                HttpContext.RequestAborted
            );

            TempData["ReservationSubmitted"] = true;
            TempData["ReservationName"] =
                reservation.FullName;

            return RedirectToAction(
                nameof(Confirmation)
            );
        }

        // CONFIRMATION
        [HttpGet]
        public IActionResult Confirmation()
        {
            if (TempData["ReservationSubmitted"] == null)
            {
                return RedirectToAction(
                    nameof(Create)
                );
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

        private async Task ValidateReservationDateTime(
            ReservationCreateViewModel model,
            DateTime nowInSkopje)
        {
            if (model.ReservationDate == default)
            {
                return;
            }

            var requestedDateTime =
                model.ReservationDate.Date
                    .Add(model.ReservationTime);

            if (requestedDateTime <=
                nowInSkopje.Add(
                    ReservationPolicy.MinimumLeadTime))
            {
                ModelState.AddModelError(
                    nameof(model.ReservationDate),
                    "Please choose a time at least 2 hours from now."
                );
            }

            if (!ReservationPolicy.IsOnlineSlot(
                    model.ReservationTime))
            {
                ModelState.AddModelError(
                    nameof(model.ReservationTime),
                    "Please choose one of the available reservation times."
                );
            }

            if (!ModelState.IsValid)
            {
                return;
            }

            var reservedCoversForSlot =
                await _context.Reservations
                    .AsNoTracking()
                    .Where(reservation =>
                        reservation.ReservationDate ==
                            model.ReservationDate.Date &&
                        reservation.ReservationTime ==
                            model.ReservationTime &&
                        reservation.Status !=
                            ReservationStatus.Rejected &&
                        reservation.Status !=
                            ReservationStatus.NoShow)
                    .SumAsync(reservation =>
                        reservation.NumberOfGuests);

            if (reservedCoversForSlot +
                model.NumberOfGuests >
                ReservationPolicy.MaxOnlineCoversPerSlot)
            {
                ModelState.AddModelError(
                    nameof(model.ReservationTime),
                    "That time is fully booked. Please choose another slot."
                );
            }
        }

        private void PrepareForm(
            DateTime nowInSkopje)
        {
            ViewBag.MinimumReservationDate =
                nowInSkopje.ToString("yyyy-MM-dd");

            ViewBag.ReservationSlots =
                ReservationPolicy.GetOnlineSlots();
        }
    }
}
