using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rebel.Domain.Enums;
using Rebel.Infrastructure.Data;
using Rebel.Web.Services;

namespace Rebel.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminReservationsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<AdminReservationsController> _logger;

        private static readonly TimeZoneInfo SkopjeTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById("Europe/Skopje");

        public AdminReservationsController(
            AppDbContext context,
            IEmailService emailService,
            ILogger<AdminReservationsController> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        // ALL RESERVATIONS
        [HttpGet]
        public async Task<IActionResult> Index(
            ReservationStatus? status,
            CancellationToken cancellationToken)
        {
            var query = _context.Reservations
                .AsNoTracking()
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(r =>
                    r.Status == status.Value);
            }

            var reservations = await query
                .OrderBy(r => r.ReservationDate)
                .ThenBy(r =>
                    r.Status == ReservationStatus.Pending ? 0 :
                    r.Status == ReservationStatus.Approved ? 1 :
                    r.Status == ReservationStatus.Arrived ? 2 :
                    r.Status == ReservationStatus.NoShow ? 3 : 4)
                .ThenBy(r => r.ReservationTime)
                .ToListAsync(cancellationToken);

            ViewBag.SelectedStatus = status;

            return View(reservations);
        }

        // TONIGHT VIEW
        [HttpGet]
        public async Task<IActionResult> Tonight(
            CancellationToken cancellationToken)
        {
            var nowInSkopje = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                SkopjeTimeZone);

            var today = nowInSkopje.Date;
            var tomorrow = today.AddDays(1);

            var reservations = await _context.Reservations
                .AsNoTracking()
                .Where(r =>
                    r.ReservationDate >= today &&
                    r.ReservationDate < tomorrow &&
                    r.Status != ReservationStatus.Rejected)
                .OrderBy(r => r.ReservationTime)
                .ThenBy(r =>
                    r.Status == ReservationStatus.Pending ? 0 :
                    r.Status == ReservationStatus.Approved ? 1 :
                    r.Status == ReservationStatus.Arrived ? 2 : 3)
                .ToListAsync(cancellationToken);

            ViewBag.TonightDate = today;

            ViewBag.TotalReservations =
                reservations.Count;

            ViewBag.TotalGuests =
                reservations.Sum(r => r.NumberOfGuests);

            ViewBag.PendingReservations =
                reservations.Count(r =>
                    r.Status == ReservationStatus.Pending);

            ViewBag.ApprovedReservations =
                reservations.Count(r =>
                    r.Status == ReservationStatus.Approved);

            ViewBag.ArrivedReservations =
                reservations.Count(r =>
                    r.Status == ReservationStatus.Arrived);

            ViewBag.NoShowReservations =
                reservations.Count(r =>
                    r.Status == ReservationStatus.NoShow);

            ViewBag.SlotCapacity =
                ReservationPolicy.MaxOnlineCoversPerSlot;

            ViewBag.SlotLoads =
                reservations
                    .Where(r =>
                        r.Status != ReservationStatus.Rejected &&
                        r.Status != ReservationStatus.NoShow)
                    .GroupBy(r => r.ReservationTime)
                    .ToDictionary(
                        group => group.Key,
                        group => group.Sum(r =>
                            r.NumberOfGuests));

            return View(reservations);
        }

        // DETAILS
        [HttpGet]
        public async Task<IActionResult> Details(
            Guid id,
            CancellationToken cancellationToken)
        {
            var reservation = await _context.Reservations
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    r => r.Id == id,
                    cancellationToken);

            if (reservation == null)
            {
                return NotFound();
            }

            return View(reservation);
        }

        // APPROVE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(
            Guid id,
            string? adminNote,
            CancellationToken cancellationToken)
        {
            return await UpdateReservationStatus(
                id,
                ReservationStatus.Approved,
                adminNote,
                cancellationToken);
        }

        // REJECT
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(
            Guid id,
            string? adminNote,
            CancellationToken cancellationToken)
        {
            return await UpdateReservationStatus(
                id,
                ReservationStatus.Rejected,
                adminNote,
                cancellationToken);
        }

        // MARK AS ARRIVED
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkArrived(
            Guid id,
            CancellationToken cancellationToken)
        {
            return await UpdateAttendanceStatus(
                id,
                ReservationStatus.Arrived,
                cancellationToken);
        }

        // MARK AS NO-SHOW
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkNoShow(
            Guid id,
            CancellationToken cancellationToken)
        {
            return await UpdateAttendanceStatus(
                id,
                ReservationStatus.NoShow,
                cancellationToken);
        }

        // DELETE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(
            Guid id,
            CancellationToken cancellationToken)
        {
            var reservation = await _context.Reservations
                .FirstOrDefaultAsync(
                    r => r.Id == id,
                    cancellationToken);

            if (reservation == null)
            {
                return NotFound();
            }

            var reservationNotifications =
                await _context.Notifications
                    .Where(n =>
                        n.ReservationId == reservation.Id)
                    .ToListAsync(cancellationToken);

            if (reservationNotifications.Count > 0)
            {
                _context.Notifications.RemoveRange(
                    reservationNotifications);
            }

            _context.Reservations.Remove(reservation);

            await _context.SaveChangesAsync(
                cancellationToken);

            TempData["SuccessMessage"] =
                "The reservation has been deleted.";

            return RedirectToAction(nameof(Index));
        }

        private async Task<IActionResult> UpdateReservationStatus(
            Guid id,
            ReservationStatus newStatus,
            string? adminNote,
            CancellationToken cancellationToken)
        {
            var reservation = await _context.Reservations
                .FirstOrDefaultAsync(
                    r => r.Id == id,
                    cancellationToken);

            if (reservation == null)
            {
                return NotFound();
            }

            if (reservation.Status !=
                ReservationStatus.Pending)
            {
                TempData["ErrorMessage"] =
                    "This reservation has already been processed.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            var normalizedAdminNote =
                string.IsNullOrWhiteSpace(adminNote)
                    ? null
                    : adminNote.Trim();

            if (normalizedAdminNote?.Length > 500)
            {
                TempData["ErrorMessage"] =
                    "The admin message cannot be longer than 500 characters.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            reservation.Status = newStatus;
            reservation.RespondedAtUtc = DateTime.UtcNow;
            reservation.AdminNote = normalizedAdminNote;

            var reservationNotifications =
                await _context.Notifications
                    .Where(n =>
                        n.ReservationId == reservation.Id)
                    .ToListAsync(cancellationToken);

            if (reservationNotifications.Count > 0)
            {
                _context.Notifications.RemoveRange(
                    reservationNotifications);
            }

            await _context.SaveChangesAsync(
                cancellationToken);

            try
            {
                var isApproved =
                    newStatus ==
                    ReservationStatus.Approved;

                var subject = isApproved
                    ? "Your table is locked in 🍻 | Rebel Rebel by Fat Kitchen"
                    : "Not this round | Rebel Rebel by Fat Kitchen";

                var htmlBody = isApproved
                    ? ReservationEmailTemplate.BuildApproved(
                        reservation.FullName,
                        reservation.ReservationDate,
                        reservation.ReservationTime,
                        reservation.NumberOfGuests,
                        "cid:rebel-logo")
                    : ReservationEmailTemplate.BuildDeclined(
                        reservation.FullName,
                        reservation.ReservationDate,
                        reservation.ReservationTime,
                        reservation.NumberOfGuests,
                        "cid:rebel-logo");

                await _emailService.SendEmailAsync(
                    reservation.Email,
                    subject,
                    htmlBody,
                    cancellationToken);

                TempData["SuccessMessage"] = isApproved
                    ? "Reservation approved and confirmation email sent."
                    : "Reservation rejected and notification email sent.";
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Reservation {ReservationId} was processed, but the email could not be sent.",
                    reservation.Id);

                TempData["ErrorMessage"] =
                    newStatus ==
                    ReservationStatus.Approved
                        ? "Reservation was approved, but the email could not be sent."
                        : "Reservation was rejected, but the email could not be sent.";
            }

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        private async Task<IActionResult> UpdateAttendanceStatus(
            Guid id,
            ReservationStatus newStatus,
            CancellationToken cancellationToken)
        {
            var reservation = await _context.Reservations
                .FirstOrDefaultAsync(
                    r => r.Id == id,
                    cancellationToken);

            if (reservation == null)
            {
                return NotFound();
            }

            if (reservation.Status ==
                    ReservationStatus.Pending ||
                reservation.Status ==
                    ReservationStatus.Rejected)
            {
                TempData["ErrorMessage"] =
                    "Only approved reservations can receive an attendance status.";

                return RedirectToAction(nameof(Tonight));
            }

            reservation.Status = newStatus;

            await _context.SaveChangesAsync(
                cancellationToken);

            TempData["SuccessMessage"] =
                newStatus == ReservationStatus.Arrived
                    ? $"{reservation.FullName} marked as arrived."
                    : $"{reservation.FullName} marked as no-show.";

            return RedirectToAction(nameof(Tonight));
        }
    }
}
