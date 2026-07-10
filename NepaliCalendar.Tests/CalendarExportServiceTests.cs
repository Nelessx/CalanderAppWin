using System;
using System.Linq;
using System.Text.RegularExpressions;
using NepaliCalendar.App.Models;
using NepaliCalendar.App.Services;
using Xunit;

namespace NepaliCalendar.Tests
{
    public class CalendarExportServiceTests
    {
        private readonly CalendarExportService _export = new();

        private static CalendarEvent[] Sample() =>
        [
            new() { Id = Guid.NewGuid(), Title = "Team, Standup", AdDate = new DateTime(2026, 5, 10), BsYear = 2083, BsMonth = 1, BsDay = 27, IsAllDay = false, TimeText = "10:00 AM", EventType = "Meeting", BadgeText = "Work" },
            new() { Id = Guid.NewGuid(), Title = "Holiday", AdDate = new DateTime(2026, 5, 12), BsYear = 2083, BsMonth = 1, BsDay = 29, IsAllDay = true },
        ];

        [Fact]
        public void Csv_HasHeader_AndRowPerEvent()
        {
            string csv = _export.ToCsv(Sample());
            var lines = csv.TrimEnd().Split('\n');

            Assert.StartsWith("Title,NepaliTitle", lines[0]);
            Assert.Equal(3, lines.Length); // header + 2
        }

        [Fact]
        public void Csv_QuotesFieldsWithCommas()
        {
            string csv = _export.ToCsv(Sample());
            Assert.Contains("\"Team, Standup\"", csv);
        }

        [Fact]
        public void Ics_IsWellFormedVCalendar()
        {
            string ics = _export.ToICalendar(Sample());
            Assert.StartsWith("BEGIN:VCALENDAR", ics);
            Assert.EndsWith("END:VCALENDAR\r\n", ics);
            Assert.Equal(2, Regex.Matches(ics, "BEGIN:VEVENT").Count);
        }

        [Fact]
        public void Ics_UsesTimedAndAllDayStarts()
        {
            string ics = _export.ToICalendar(Sample());
            Assert.Contains("DTSTART:20260510T100000", ics);       // parsed time
            Assert.Contains("DTSTART;VALUE=DATE:20260512", ics);   // all-day
        }

        [Fact]
        public void Ics_EscapesText()
        {
            var evt = new CalendarEvent { Title = "A; B, C", AdDate = new DateTime(2026, 5, 10), BsYear = 2083, BsMonth = 1, BsDay = 27, IsAllDay = true };
            string ics = _export.ToICalendar([evt]);
            Assert.Contains(@"SUMMARY:A\; B\, C", ics);
        }

        [Fact]
        public void Csv_HasIsHolidayColumn_MarkedPerRow()
        {
            var events = new[]
            {
                new CalendarEvent { Title = "Meeting", AdDate = new DateTime(2026, 5, 10), BsYear = 2083, BsMonth = 1, BsDay = 27, IsHoliday = false },
                new CalendarEvent { Title = "Dashain", AdDate = new DateTime(2026, 5, 12), BsYear = 2083, BsMonth = 1, BsDay = 29, IsHoliday = true, IsAllDay = true },
            };

            string csv = _export.ToCsv(events);
            var lines = csv.TrimEnd().Split('\n');

            Assert.EndsWith("IsHoliday", lines[0].TrimEnd());
            Assert.EndsWith("No", lines[1].TrimEnd());   // meeting
            Assert.EndsWith("Yes", lines[2].TrimEnd());  // holiday
        }

        [Fact]
        public void Ics_TagsHolidaysWithCategory()
        {
            var events = new[]
            {
                new CalendarEvent { Title = "Meeting", AdDate = new DateTime(2026, 5, 10), BsYear = 2083, BsMonth = 1, BsDay = 27 },
                new CalendarEvent { Title = "Dashain", AdDate = new DateTime(2026, 5, 12), BsYear = 2083, BsMonth = 1, BsDay = 29, IsHoliday = true, IsAllDay = true },
            };

            string ics = _export.ToICalendar(events);
            Assert.Single(Regex.Matches(ics, "CATEGORIES:HOLIDAY"));
        }
    }
}
