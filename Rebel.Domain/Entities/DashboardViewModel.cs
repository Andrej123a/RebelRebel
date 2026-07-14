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

        public List<Event> UpcomingEvents { get; set; } = new();
    }
}