using Rebel.Domain.Entities;

namespace Rebel.Web.Models
{
    public class DashboardViewModel
    {
        public int CategoriesCount { get; set; }

        public int ProductsCount { get; set; }

        public int EventsCount { get; set; }

        public int ReservationsCount { get; set; }

        public int PendingReservationsCount { get; set; }

        public int TonightReservationsCount { get; set; }

        public int TonightGuestsCount { get; set; }

        public int TonightPendingReservationsCount { get; set; }

        public int TonightUnassignedTablesCount { get; set; }

        public int UnavailableProductsCount { get; set; }

        public List<Reservation> LatestPendingReservations { get; set; } = new();

        public List<Product> UnavailableProducts { get; set; } = new();

        public List<Event> UpcomingEvents { get; set; } = new();
    }
}
