using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using NepaliCalendar.App.Models;

namespace NepaliCalendar.App.Services
{
    /// <summary>
    /// Serializes calendar events to portable formats. Pure string transforms
    /// (no file IO) so they can be unit-tested and reused.
    /// </summary>
    public class CalendarExportService
    {
        /// <summary>Exports events as CSV (RFC 4180 quoting).</summary>
        public string ToCsv(IEnumerable<CalendarEvent> events)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Title,NepaliTitle,BsDate,AdDate,Weekday,Time,AllDay,Type,Badge,IsHoliday");

            foreach (var e in events.OrderBy(e => e.AdDate))
            {
                string bsDate = $"{e.BsYear:D4}-{e.BsMonth:D2}-{e.BsDay:D2}";
                string adDate = e.AdDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                string weekday = e.AdDate.DayOfWeek.ToString();
                string time = e.IsAllDay ? "All Day" : (e.TimeText ?? string.Empty);

                sb.AppendLine(string.Join(",",
                    Csv(e.Title),
                    Csv(e.NepaliTitle),
                    Csv(bsDate),
                    Csv(adDate),
                    Csv(weekday),
                    Csv(time),
                    Csv(e.IsAllDay ? "Yes" : "No"),
                    Csv(e.EventType),
                    Csv(e.BadgeText),
                    Csv(e.IsHoliday ? "Yes" : "No")));
            }

            return sb.ToString();
        }

        /// <summary>Exports events as an RFC 5545 iCalendar (.ics) document.</summary>
        public string ToICalendar(IEnumerable<CalendarEvent> events)
        {
            var sb = new StringBuilder();
            sb.Append("BEGIN:VCALENDAR\r\n");
            sb.Append("VERSION:2.0\r\n");
            sb.Append("PRODID:-//Nepali Calendar//EN\r\n");
            sb.Append("CALSCALE:GREGORIAN\r\n");

            string stamp = DateTime.UtcNow.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);

            foreach (var e in events.OrderBy(e => e.AdDate))
            {
                sb.Append("BEGIN:VEVENT\r\n");
                sb.Append(Fold($"UID:{(e.Id == Guid.Empty ? Guid.NewGuid() : e.Id)}@nepalicalendar"));
                sb.Append(Fold($"DTSTAMP:{stamp}"));

                // Emit a timed event only when the free-text time parses cleanly; otherwise all-day.
                if (!e.IsAllDay && TryParseTime(e.TimeText, out var timeOfDay))
                {
                    var start = e.AdDate.Date + timeOfDay;
                    var end = start.AddHours(1);
                    sb.Append(Fold($"DTSTART:{start:yyyyMMdd'T'HHmmss}"));
                    sb.Append(Fold($"DTEND:{end:yyyyMMdd'T'HHmmss}"));
                }
                else
                {
                    sb.Append(Fold($"DTSTART;VALUE=DATE:{e.AdDate:yyyyMMdd}"));
                    sb.Append(Fold($"DTEND;VALUE=DATE:{e.AdDate.AddDays(1):yyyyMMdd}"));
                }

                sb.Append(Fold($"SUMMARY:{Escape(e.Title)}"));

                if (e.IsHoliday)
                    sb.Append(Fold("CATEGORIES:HOLIDAY"));

                string description = BuildDescription(e);
                if (!string.IsNullOrEmpty(description))
                    sb.Append(Fold($"DESCRIPTION:{Escape(description)}"));

                sb.Append("END:VEVENT\r\n");
            }

            sb.Append("END:VCALENDAR\r\n");
            return sb.ToString();
        }

        private static string BuildDescription(CalendarEvent e)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(e.NepaliTitle)) parts.Add(e.NepaliTitle!);
            parts.Add($"BS {e.BsYear:D4}-{e.BsMonth:D2}-{e.BsDay:D2}");
            if (!e.IsAllDay && !string.IsNullOrWhiteSpace(e.TimeText)) parts.Add(e.TimeText!);
            if (!string.IsNullOrWhiteSpace(e.EventType)) parts.Add(e.EventType);
            if (!string.IsNullOrWhiteSpace(e.BadgeText)) parts.Add(e.BadgeText!);
            return string.Join(" | ", parts);
        }

        private static bool TryParseTime(string? timeText, out TimeSpan timeOfDay)
        {
            if (!string.IsNullOrWhiteSpace(timeText) &&
                DateTime.TryParse(timeText, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            {
                timeOfDay = parsed.TimeOfDay;
                return true;
            }

            timeOfDay = default;
            return false;
        }

        private static string Csv(string? value)
        {
            value ??= string.Empty;

            if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
                return "\"" + value.Replace("\"", "\"\"") + "\"";

            return value;
        }

        // RFC 5545 text escaping for property values.
        private static string Escape(string value)
        {
            return value
                .Replace("\\", "\\\\")
                .Replace(";", "\\;")
                .Replace(",", "\\,")
                .Replace("\r\n", "\\n")
                .Replace("\n", "\\n")
                .Replace("\r", "\\n");
        }

        // RFC 5545 line folding at 75 octets, continuation lines start with a space.
        private static string Fold(string line)
        {
            const int max = 75;

            if (line.Length <= max)
                return line + "\r\n";

            var sb = new StringBuilder();
            int index = 0;

            sb.Append(line.AsSpan(0, max)).Append("\r\n");
            index = max;

            while (index < line.Length)
            {
                int take = Math.Min(max - 1, line.Length - index);
                sb.Append(' ').Append(line.AsSpan(index, take)).Append("\r\n");
                index += take;
            }

            return sb.ToString();
        }
    }
}
