using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using NepaliCalendar.App.Models;

namespace NepaliCalendar.App.Services
{
    /// <summary>
    /// Provides Nepali holidays from Data/holidays.json, expanded to concrete
    /// dates for a given BS year and surfaced as <see cref="CalendarEvent"/> so the
    /// dashboard/calendar can render them alongside user events.
    /// </summary>
    public class HolidayService
    {
        private readonly string _filePath;
        private readonly BsDateConverter _converter = new();
        private List<Holiday>? _cache;

        public HolidayService()
        {
            _filePath = Path.Combine(AppContext.BaseDirectory, "Data", "holidays.json");
        }

        private List<Holiday> LoadAll()
        {
            if (_cache != null)
                return _cache;

            try
            {
                if (!File.Exists(_filePath))
                    return _cache = new List<Holiday>();

                string json = File.ReadAllText(_filePath);
                var holidays = JsonSerializer.Deserialize<List<Holiday>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return _cache = holidays ?? new List<Holiday>();
            }
            catch
            {
                return _cache = new List<Holiday>();
            }
        }

        /// <summary>All holidays that fall in the given BS year, as dated CalendarEvents.</summary>
        public List<CalendarEvent> GetHolidaysForBsYear(int bsYear)
        {
            var result = new List<CalendarEvent>();

            foreach (var holiday in LoadAll())
            {
                if (holiday.BsYear != null && holiday.BsYear != bsYear)
                    continue;

                int year = holiday.BsYear ?? bsYear;

                DateTime adDate;
                try
                {
                    adDate = _converter.ConvertToAd(year, holiday.BsMonth, holiday.BsDay);
                }
                catch
                {
                    continue; // outside the supported data range
                }

                result.Add(new CalendarEvent
                {
                    Title = holiday.Title,
                    NepaliTitle = holiday.NepaliTitle,
                    AdDate = adDate,
                    BsYear = year,
                    BsMonth = holiday.BsMonth,
                    BsDay = holiday.BsDay,
                    EventType = "Holiday",
                    BadgeText = holiday.IsPublic ? "Public Holiday" : "Holiday",
                    IsHoliday = true,
                    IsPublicHoliday = holiday.IsPublic,
                    IsAllDay = true,
                    TimeText = "All Day",
                    DayText = adDate.DayOfWeek.ToString()
                });
            }

            return result;
        }

        public List<CalendarEvent> GetForBsDate(int year, int month, int day)
        {
            return GetHolidaysForBsYear(year)
                .Where(h => h.BsMonth == month && h.BsDay == day)
                .ToList();
        }

        /// <summary>Upcoming holidays from a given AD date, spanning the current and next BS year.</summary>
        public List<CalendarEvent> GetUpcoming(int bsYear, DateTime fromAdDate, int max)
        {
            return GetHolidaysForBsYear(bsYear)
                .Concat(GetHolidaysForBsYear(bsYear + 1))
                .Where(h => h.AdDate.Date >= fromAdDate.Date)
                .OrderBy(h => h.AdDate)
                .Take(max)
                .ToList();
        }
    }
}
