using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using NepaliCalendar.App.Models;

namespace NepaliCalendar.App.Services
{
    /// <summary>
    /// Parses events out of iCalendar (.ics) and CSV files so users can migrate from Google/
    /// Outlook or restore an exported backup. Gregorian dates are converted to Bikram Sambat with
    /// the app's own engine; anything outside the supported range is skipped rather than failing
    /// the whole import.
    /// </summary>
    public class CalendarImportService
    {
        private readonly BsDateConverter _converter = new();

        public List<CalendarEvent> Parse(string content, bool isCsv) =>
            isCsv ? FromCsv(content) : FromICalendar(content);

        // --- iCalendar ---
        public List<CalendarEvent> FromICalendar(string ics)
        {
            var events = new List<CalendarEvent>();
            var lines = Unfold(ics);

            CalendarEvent? current = null;
            foreach (var line in lines)
            {
                if (line.StartsWith("BEGIN:VEVENT", StringComparison.OrdinalIgnoreCase))
                {
                    current = new CalendarEvent { EventType = "Event" };
                    continue;
                }

                if (line.StartsWith("END:VEVENT", StringComparison.OrdinalIgnoreCase))
                {
                    if (current != null && TryFinalize(current))
                        events.Add(current);
                    current = null;
                    continue;
                }

                if (current is null)
                    continue;

                var (name, value) = SplitProperty(line);
                switch (name)
                {
                    case "SUMMARY":
                        current.Title = Unescape(value);
                        break;
                    case "LOCATION":
                        current.Location = Unescape(value);
                        break;
                    case "DESCRIPTION":
                        current.Notes = Unescape(value);
                        break;
                    case "DTSTART":
                        ApplyStart(current, line);
                        break;
                }
            }

            return events;
        }

        private bool TryFinalize(CalendarEvent e)
        {
            if (string.IsNullOrWhiteSpace(e.Title) || e.AdDate == default)
                return false;

            if (!_converter.TryConvertFromAd(e.AdDate, out var bs) || bs is null)
                return false; // outside supported BS range

            e.BsYear = bs.Year;
            e.BsMonth = bs.Month;
            e.BsDay = bs.Day;
            e.DayText = e.AdDate.DayOfWeek.ToString();
            return true;
        }

        private static void ApplyStart(CalendarEvent e, string line)
        {
            // Forms: DTSTART:20260510T093000  /  DTSTART;VALUE=DATE:20260510
            var (_, value) = SplitProperty(line);
            bool isDateOnly = line.IndexOf("VALUE=DATE", StringComparison.OrdinalIgnoreCase) >= 0 && value.Length == 8;

            if (isDateOnly && DateTime.TryParseExact(value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            {
                e.AdDate = d.Date;
                e.IsAllDay = true;
                return;
            }

            string trimmed = value.TrimEnd('Z');
            if (DateTime.TryParseExact(trimmed, "yyyyMMdd'T'HHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            {
                e.AdDate = dt.Date;
                e.IsAllDay = false;
                e.TimeText = dt.ToString("hh:mm tt", CultureInfo.InvariantCulture);
                return;
            }

            if (DateTime.TryParseExact(value.Length >= 8 ? value.Substring(0, 8) : value, "yyyyMMdd",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var fallback))
            {
                e.AdDate = fallback.Date;
                e.IsAllDay = true;
            }
        }

        // --- CSV (the app's own export columns; ignores holiday rows) ---
        public List<CalendarEvent> FromCsv(string csv)
        {
            var events = new List<CalendarEvent>();
            var rows = csv.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (rows.Length < 2)
                return events;

            var header = ParseCsvLine(rows[0]);
            int Idx(string col) => Array.FindIndex(header, h => h.Equals(col, StringComparison.OrdinalIgnoreCase));

            int iTitle = Idx("Title"), iNepali = Idx("NepaliTitle"), iAd = Idx("AdDate"),
                iTime = Idx("Time"), iAllDay = Idx("AllDay"), iType = Idx("Type"),
                iBadge = Idx("Badge"), iLoc = Idx("Location"), iNotes = Idx("Notes"),
                iHoliday = Idx("IsHoliday");

            for (int r = 1; r < rows.Length; r++)
            {
                var f = ParseCsvLine(rows[r]);
                if (iTitle < 0 || iAd < 0 || iTitle >= f.Length || iAd >= f.Length)
                    continue;

                // Skip holiday rows — holidays come from the app's own data set.
                if (iHoliday >= 0 && iHoliday < f.Length && f[iHoliday].Equals("Yes", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!DateTime.TryParse(f[iAd], CultureInfo.InvariantCulture, DateTimeStyles.None, out var ad))
                    continue;

                if (!_converter.TryConvertFromAd(ad.Date, out var bs) || bs is null)
                    continue;

                bool allDay = iAllDay >= 0 && iAllDay < f.Length && f[iAllDay].Equals("Yes", StringComparison.OrdinalIgnoreCase);
                string? time = iTime >= 0 && iTime < f.Length && !string.IsNullOrWhiteSpace(f[iTime]) && !f[iTime].Equals("All Day", StringComparison.OrdinalIgnoreCase)
                    ? f[iTime] : null;

                events.Add(new CalendarEvent
                {
                    Title = f[iTitle],
                    NepaliTitle = iNepali >= 0 && iNepali < f.Length && !string.IsNullOrWhiteSpace(f[iNepali]) ? f[iNepali] : null,
                    AdDate = ad.Date,
                    BsYear = bs.Year,
                    BsMonth = bs.Month,
                    BsDay = bs.Day,
                    EventType = iType >= 0 && iType < f.Length && !string.IsNullOrWhiteSpace(f[iType]) ? f[iType] : "Event",
                    BadgeText = iBadge >= 0 && iBadge < f.Length && !string.IsNullOrWhiteSpace(f[iBadge]) ? f[iBadge] : null,
                    Location = iLoc >= 0 && iLoc < f.Length && !string.IsNullOrWhiteSpace(f[iLoc]) ? f[iLoc] : null,
                    Notes = iNotes >= 0 && iNotes < f.Length && !string.IsNullOrWhiteSpace(f[iNotes]) ? f[iNotes] : null,
                    IsAllDay = allDay || time is null,
                    TimeText = time,
                    DayText = ad.DayOfWeek.ToString()
                });
            }

            return events;
        }

        // --- helpers ---
        private static List<string> Unfold(string text)
        {
            var raw = text.Replace("\r\n", "\n").Split('\n');
            var result = new List<string>();

            foreach (var line in raw)
            {
                if ((line.StartsWith(" ") || line.StartsWith("\t")) && result.Count > 0)
                    result[^1] += line.Substring(1);
                else
                    result.Add(line);
            }

            return result;
        }

        private static (string Name, string Value) SplitProperty(string line)
        {
            int colon = line.IndexOf(':');
            if (colon < 0)
                return (line, string.Empty);

            string left = line.Substring(0, colon);
            int semi = left.IndexOf(';');
            string name = (semi >= 0 ? left.Substring(0, semi) : left).ToUpperInvariant();
            return (name, line.Substring(colon + 1));
        }

        private static string Unescape(string v) =>
            v.Replace("\\n", "\n").Replace("\\,", ",").Replace("\\;", ";").Replace("\\\\", "\\");

        private static string[] ParseCsvLine(string line)
        {
            var fields = new List<string>();
            var sb = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; }
                        else inQuotes = false;
                    }
                    else sb.Append(c);
                }
                else
                {
                    if (c == '"') inQuotes = true;
                    else if (c == ',') { fields.Add(sb.ToString()); sb.Clear(); }
                    else sb.Append(c);
                }
            }

            fields.Add(sb.ToString());
            return fields.ToArray();
        }
    }
}
