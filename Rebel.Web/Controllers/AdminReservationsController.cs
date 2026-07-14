using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rebel.Domain.Entities;
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

        public AdminReservationsController(
            AppDbContext context,
            IEmailService emailService,
            ILogger<AdminReservationsController> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        // LIST
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
                query = query.Where(r => r.Status == status.Value);
            }

            var reservations = await query
                .OrderBy(r =>
                    r.Status == ReservationStatus.Pending ? 0 :
                    r.Status == ReservationStatus.Approved ? 1 : 2)
                .ThenBy(r => r.ReservationDate)
                .ThenBy(r => r.ReservationTime)
                .ToListAsync(cancellationToken);

            ViewBag.SelectedStatus = status;

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
            return await UpdateStatus(
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
            return await UpdateStatus(
                id,
                ReservationStatus.Rejected,
                adminNote,
                cancellationToken);
        }

        private async Task<IActionResult> UpdateStatus(
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

            if (reservation.Status != ReservationStatus.Pending)
            {
                TempData["ErrorMessage"] =
                    "This reservation has already been processed.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }

            var normalizedAdminNote = string.IsNullOrWhiteSpace(adminNote)
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

            await _context.SaveChangesAsync(cancellationToken);

            try
            {
                var subject = newStatus == ReservationStatus.Approved
                    ? "Your Rebel Rebel reservation is confirmed"
                    : "Update regarding your Rebel Rebel reservation";

                var htmlBody = BuildReservationEmail(
                    reservation,
                    newStatus);

                await _emailService.SendEmailAsync(
                    reservation.Email,
                    subject,
                    htmlBody,
                    cancellationToken);

                TempData["SuccessMessage"] =
                    newStatus == ReservationStatus.Approved
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
                    newStatus == ReservationStatus.Approved
                        ? "Reservation was approved, but the email could not be sent."
                        : "Reservation was rejected, but the email could not be sent.";
            }

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        private static string BuildReservationEmail(
            Reservation reservation,
            ReservationStatus status)
        {
            var fullName = WebUtility.HtmlEncode(reservation.FullName);
            var reservationDate =
                reservation.ReservationDate.ToString("dd MMMM yyyy");

            var reservationTime =
                reservation.ReservationTime.ToString(@"hh\:mm");

            var numberOfGuests = reservation.NumberOfGuests;

            var adminNote = string.IsNullOrWhiteSpace(reservation.AdminNote)
                ? null
                : WebUtility.HtmlEncode(reservation.AdminNote);

            var isApproved =
                status == ReservationStatus.Approved;

            var accentColor = isApproved
                ? "#f4bd00"
                : "#c62828";

            var heading = isApproved
                ? "YOUR TABLE IS CONFIRMED"
                : "RESERVATION UPDATE";

            var mainMessage = isApproved
                ? "Your reservation request has been approved. We are looking forward to seeing you at Rebel Rebel!"
                : "Unfortunately, we are unable to confirm your reservation request for the selected date and time.";

            var adminNoteSection = adminNote == null
                ? string.Empty
                : $"""
                   <div style="margin-top:24px;padding:18px;background:#171717;border-left:4px solid {accentColor};">
                       <div style="color:#888888;font-size:12px;font-weight:bold;letter-spacing:1.5px;text-transform:uppercase;margin-bottom:8px;">
                           Message from Rebel Rebel
                       </div>

                       <div style="color:#ffffff;font-size:15px;line-height:1.6;">
                           {adminNote}
                       </div>
                   </div>
                   """;

            return $"""
                <!DOCTYPE html>
                <html lang="en">
                <head>
                    <meta charset="UTF-8">
                    <meta name="viewport" content="width=device-width, initial-scale=1.0">
                    <title>Rebel Rebel Reservation</title>
                </head>

                <body style="margin:0;padding:0;background:#080808;font-family:Arial,Helvetica,sans-serif;color:#ffffff;">

                    <table role="presentation"
                           width="100%"
                           cellspacing="0"
                           cellpadding="0"
                           style="background:#080808;padding:35px 15px;">

                        <tr>
                            <td align="center">

                                <table role="presentation"
                                       width="100%"
                                       cellspacing="0"
                                       cellpadding="0"
                                       style="max-width:650px;background:#111111;border:1px solid #333333;">

                                    <tr>
                                        <td style="padding:35px 40px;background:#000000;border-bottom:3px solid {accentColor};">
                                            <div style="color:{accentColor};font-size:13px;font-weight:bold;letter-spacing:3px;text-transform:uppercase;">
                                                Rebel Rebel
                                            </div>

                                            <div style="margin-top:10px;font-size:30px;font-weight:900;line-height:1.1;">
                                                {heading}
                                            </div>
                                        </td>
                                    </tr>

                                    <tr>
                                        <td style="padding:40px;">

                                            <p style="margin:0 0 18px;font-size:18px;line-height:1.6;">
                                                Hi <strong>{fullName}</strong>,
                                            </p>

                                            <p style="margin:0 0 30px;color:#bbbbbb;font-size:16px;line-height:1.7;">
                                                {mainMessage}
                                            </p>

                                            <table role="presentation"
                                                   width="100%"
                                                   cellspacing="0"
                                                   cellpadding="0"
                                                   style="background:#191919;border-collapse:collapse;">

                                                <tr>
                                                    <td style="padding:14px 18px;border-bottom:1px solid #333333;color:#888888;font-size:12px;font-weight:bold;text-transform:uppercase;">
                                                        Date
                                                    </td>

                                                    <td align="right"
                                                        style="padding:14px 18px;border-bottom:1px solid #333333;color:#ffffff;font-weight:bold;">
                                                        {reservationDate}
                                                    </td>
                                                </tr>

                                                <tr>
                                                    <td style="padding:14px 18px;border-bottom:1px solid #333333;color:#888888;font-size:12px;font-weight:bold;text-transform:uppercase;">
                                                        Time
                                                    </td>

                                                    <td align="right"
                                                        style="padding:14px 18px;border-bottom:1px solid #333333;color:#ffffff;font-weight:bold;">
                                                        {reservationTime}
                                                    </td>
                                                </tr>

                                                <tr>
                                                    <td style="padding:14px 18px;color:#888888;font-size:12px;font-weight:bold;text-transform:uppercase;">
                                                        Guests
                                                    </td>

                                                    <td align="right"
                                                        style="padding:14px 18px;color:#ffffff;font-weight:bold;">
                                                        {numberOfGuests}
                                                    </td>
                                                </tr>

                                            </table>

                                            {adminNoteSection}

                                            <p style="margin:30px 0 0;color:#777777;font-size:13px;line-height:1.6;">
                                                This is an automated message regarding your Rebel Rebel reservation request.
                                            </p>

                                        </td>
                                    </tr>

                                    <tr>
                                        <td align="center"
                                            style="padding:22px;background:#000000;color:#666666;font-size:12px;letter-spacing:1px;text-transform:uppercase;">
                                            Rebel Rebel · No boring nights
                                        </td>
                                    </tr>

                                </table>

                            </td>
                        </tr>

                    </table>

                </body>
                </html>
                """;
        }
    }
}