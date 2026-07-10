using System;

namespace NepaliCalendar.App.Models
{
    public class CalendarEvent
    {
        public Guid Id { get; set; }

        public string Title { get; set; } = string.Empty;
        public string? NepaliTitle { get; set; }

        public DateTime AdDate { get; set; }

        public int BsYear { get; set; }
        public int BsMonth { get; set; }
        public int BsDay { get; set; }

        public string EventType { get; set; } = string.Empty;
        public string? BadgeText { get; set; }

        public bool IsAllDay { get; set; }
        public string? TimeText { get; set; }

        public bool IsHoliday { get; set; }
        public bool IsPublicHoliday { get; set; }

        /// <summary>Holiday classification (National/Religious/Cultural/Observance); null for user events.</summary>
        public string? Category { get; set; }

        /// <summary>Region a holiday is limited to (e.g. "Kathmandu Valley"); null = nationwide.</summary>
        public string? Region { get; set; }

        public string? DayText { get; set; }

        public DateTime CreatedUtc { get; set; }
        public DateTime ModifiedUtc { get; set; }
    }
}