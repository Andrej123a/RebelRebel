using System.ComponentModel.DataAnnotations;
using Rebel.Domain.Enums;

namespace Rebel.Domain.Entities
{
    public class Reservation
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(100)]
        public string FullName { get; set; } = null!;

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [StringLength(150)]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Phone number is required.")]
        [Phone(ErrorMessage = "Please enter a valid phone number.")]
        [StringLength(30)]
        public string PhoneNumber { get; set; } = null!;

        [Required(ErrorMessage = "Reservation date is required.")]
        public DateTime ReservationDate { get; set; }

        [Required(ErrorMessage = "Reservation time is required.")]
        public TimeSpan ReservationTime { get; set; }

        [Range(
            1,
            20,
            ErrorMessage = "Number of guests must be between 1 and 20."
        )]
        public int NumberOfGuests { get; set; }

        [StringLength(500)]
        public string? Note { get; set; }

        public ReservationStatus Status { get; set; }
            = ReservationStatus.Pending;

        public DateTime CreatedAtUtc { get; set; }
            = DateTime.UtcNow;

        public DateTime? RespondedAtUtc { get; set; }

        [StringLength(500)]
        public string? AdminNote { get; set; }
        public Guid? EventId { get; set; }

        public Event? Event { get; set; }
    }
}