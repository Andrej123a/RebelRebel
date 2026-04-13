using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rebel.Domain.Enums;

namespace Rebel.Domain.Entities
{
    public class Event
    {
        public Guid Id { get; set; }

        public string Title { get; set; } = null!;

        public string Description { get; set; } = null!;

        public DateTime Date { get; set; }

        public string? ImageUrl { get; set; }

        public bool IsActive { get; set; } = true;

        public TimeSpan? StartTime { get; set; }

        public TimeSpan? EndTime { get; set; }
        public EventType EventType { get; set; }
    }
}
