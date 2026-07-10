using System;
using System.Collections.Generic;
using System.Linq;
using NepaliCalendar.App.Models;

namespace NepaliCalendar.App.Services
{
    public class BsDateConverter
    {
        private readonly DateTime _referenceAdDate = new(2024, 4, 13);
        private readonly BsDate _referenceBsDate = new()
        {
            Year = 2081,
            Month = 1,
            Day = 1,
            DayName = "Saturday"
        };

        private readonly NepaliNumberService _nepaliNumberService = new();
        private readonly BsCalendarDataService _dataService = new();

        public BsDate ConvertFromAd(DateTime adDate)
        {
            if (adDate < _referenceAdDate)
                throw new NotSupportedException("Dates before the reference date are not supported yet.");

            int offsetDays = (adDate - _referenceAdDate).Days;

            int year = _referenceBsDate.Year;
            int month = _referenceBsDate.Month;
            int day = _referenceBsDate.Day;

            while (offsetDays > 0)
            {
                var yearData = GetYearData(year);

                day++;

                if (day > yearData.MonthDays[month - 1])
                {
                    day = 1;
                    month++;

                    if (month > 12)
                    {
                        month = 1;
                        year++;
                    }
                }

                offsetDays--;
            }

            return new BsDate
            {
                Year = year,
                Month = month,
                Day = day,
                DayName = adDate.DayOfWeek.ToString()
            };
        }

        /// <summary>
        /// Attempts to convert an AD date to BS without throwing when the date falls
        /// outside the loaded data range. Returns false (and null) instead of throwing.
        /// </summary>
        public bool TryConvertFromAd(DateTime adDate, out BsDate? bsDate)
        {
            try
            {
                bsDate = ConvertFromAd(adDate);
                return true;
            }
            catch
            {
                bsDate = null;
                return false;
            }
        }

        /// <summary>True if the given AD date can be represented in the loaded BS data range.</summary>
        public bool IsAdDateSupported(DateTime adDate) => TryConvertFromAd(adDate, out _);

        public DateTime ConvertToAd(int bsYear, int bsMonth, int bsDay)
        {
            if (bsYear < _referenceBsDate.Year)
                throw new NotSupportedException("BS years before the reference year are not supported yet.");

            int totalOffset = 0;

            for (int year = _referenceBsDate.Year; year < bsYear; year++)
            {
                var yearData = GetYearData(year);
                totalOffset += yearData.MonthDays.Sum();
            }

            var targetYearData = GetYearData(bsYear);

            for (int month = 1; month < bsMonth; month++)
            {
                totalOffset += targetYearData.MonthDays[month - 1];
            }

            totalOffset += bsDay - 1;

            return _referenceAdDate.AddDays(totalOffset);
        }

        public List<CalendarDay> GetMonthDays(int year, int month)
        {
            var yearData = GetYearData(year);
            int totalDays = yearData.MonthDays[month - 1];

            // Today may fall outside the loaded range (e.g. far-future clock); if so,
            // simply mark nothing as "today" rather than throwing while rendering a month.
            TryConvertFromAd(DateTime.Today, out var todayBs);

            var days = new List<CalendarDay>();

            for (int day = 1; day <= totalDays; day++)
            {
                var adDate = ConvertToAd(year, month, day);

                days.Add(new CalendarDay
                {
                    Year = year,
                    Month = month,
                    Day = day,
                    DayName = adDate.DayOfWeek.ToString(),
                    IsToday = todayBs != null && todayBs.Year == year && todayBs.Month == month && todayBs.Day == day
                });
            }

            return days;
        }

        public List<CalendarCell> GetMonthGrid(int year, int month, bool useNepaliNumbers)
        {
            var monthDays = GetMonthDays(year, month);
            var firstDayAd = ConvertToAd(year, month, 1);
            int leadingBlanks = (int)firstDayAd.DayOfWeek;

            var cells = new List<CalendarCell>();

            for (int i = 0; i < leadingBlanks; i++)
            {
                cells.Add(new CalendarCell
                {
                    Text = "",
                    DayName = "",
                    IsCurrentMonth = false
                });
            }

            foreach (var day in monthDays)
            {
                var adDate = ConvertToAd(day.Year, day.Month, day.Day);

                cells.Add(new CalendarCell
                {
                    Text = useNepaliNumbers ? _nepaliNumberService.ToNepaliNumber(day.Day) : day.Day.ToString(),
                    DayName = day.DayName,
                    IsToday = day.IsToday,
                    IsCurrentMonth = true,
                    DayOfWeekIndex = (int)adDate.DayOfWeek,
                    Year = day.Year,
                    Month = day.Month,
                    Day = day.Day
                });
            }

            while (cells.Count % 7 != 0)
            {
                cells.Add(new CalendarCell
                {
                    Text = "",
                    DayName = "",
                    IsCurrentMonth = false
                });
            }

            return cells;
        }

        public string GetNepaliMonthName(int month)
        {
            return month switch
            {
                1 => "Baisakh",
                2 => "Jestha",
                3 => "Ashadh",
                4 => "Shrawan",
                5 => "Bhadra",
                6 => "Ashwin",
                7 => "Kartik",
                8 => "Mangsir",
                9 => "Poush",
                10 => "Magh",
                11 => "Falgun",
                12 => "Chaitra",
                _ => "Unknown"
            };
        }

        public List<int> GetAvailableYears()
        {
            return _dataService.GetYears()
                .Select(y => y.Year)
                .OrderBy(y => y)
                .ToList();
        }

        private BsYearData GetYearData(int year)
        {
            var yearData = _dataService.GetYears().FirstOrDefault(y => y.Year == year);

            if (yearData == null)
                throw new Exception($"Missing BS data for year {year}.");

            return yearData;
        }
    }
}