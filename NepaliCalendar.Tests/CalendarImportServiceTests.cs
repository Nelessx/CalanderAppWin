using System;
using System.Linq;
using NepaliCalendar.App.Models;
using NepaliCalendar.App.Services;
using Xunit;

namespace NepaliCalendar.Tests
{
    public class CalendarImportServiceTests
    {
        private readonly CalendarImportService _import = new();
        private readonly CalendarExportService _export = new();

        [Fact]
        public void Ics_ParsesTimedAndAllDayEvents_WithBsConversion()
        {
            string ics =
                "BEGIN:VCALENDAR\r\n" +
                "BEGIN:VEVENT\r\nSUMMARY:Meeting\r\nDTSTART:20260510T093000\r\nLOCATION:Office\r\nEND:VEVENT\r\n" +
                "BEGIN:VEVENT\r\nSUMMARY:Day Off\r\nDTSTART;VALUE=DATE:20260512\r\nEND:VEVENT\r\n" +
                "END:VCALENDAR\r\n";

            var events = _import.FromICalendar(ics);

            Assert.Equal(2, events.Count);

            var meeting = events[0];
            Assert.Equal("Meeting", meeting.Title);
            Assert.False(meeting.IsAllDay);
            Assert.Equal("Office", meeting.Location);
            Assert.Equal(new DateTime(2026, 5, 10), meeting.AdDate);
            Assert.True(meeting.BsYear >= 2081);

            Assert.True(events[1].IsAllDay);
        }

        [Fact]
        public void Csv_RoundTripsThroughExport()
        {
            var original = new[]
            {
                new CalendarEvent { Title = "Standup", AdDate = new DateTime(2026, 5, 10), BsYear = 2083, BsMonth = 1, BsDay = 27, TimeText = "10:00 AM", EventType = "Meeting", BadgeText = "Work", Location = "HQ" },
                new CalendarEvent { Title = "Trip", AdDate = new DateTime(2026, 6, 1), BsYear = 2083, BsMonth = 2, BsDay = 18, IsAllDay = true },
            };

            string csv = _export.ToCsv(original);
            var back = _import.FromCsv(csv);

            Assert.Equal(2, back.Count);
            Assert.Contains(back, e => e.Title == "Standup" && e.Location == "HQ" && !e.IsAllDay);
            Assert.Contains(back, e => e.Title == "Trip" && e.IsAllDay);
        }

        [Fact]
        public void Csv_SkipsHolidayRows()
        {
            var mixed = new[]
            {
                new CalendarEvent { Title = "My Event", AdDate = new DateTime(2026, 5, 10), BsYear = 2083, BsMonth = 1, BsDay = 27 },
                new CalendarEvent { Title = "Dashain", AdDate = new DateTime(2026, 10, 20), BsYear = 2083, BsMonth = 7, BsDay = 4, IsHoliday = true, IsAllDay = true },
            };

            string csv = _export.ToCsv(mixed);
            var back = _import.FromCsv(csv);

            Assert.Single(back);
            Assert.Equal("My Event", back[0].Title);
        }

        [Fact]
        public void Ics_SkipsEventsOutsideSupportedRange()
        {
            // Year 1990 is far below the BS data range and cannot be represented.
            string ics = "BEGIN:VEVENT\r\nSUMMARY:Ancient\r\nDTSTART;VALUE=DATE:19900101\r\nEND:VEVENT\r\n";
            Assert.Empty(_import.FromICalendar(ics));
        }
    }
}
