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
        private bool _referenceVerified;

        // Days from the reference AD date (Baisakh 1 of the reference year) to Baisakh 1 of each
        // supported BS year, precomputed once. Turns conversion from an O(days) day-by-day walk
        // — which ran per calendar cell — into an O(months) index lookup.
        private Dictionary<int, int>? _yearStartOffset;

        private void EnsureIndex()
        {
            if (_yearStartOffset != null)
                return;

            VerifyReferenceMatchesData();

            var years = _dataService.GetYears(); // ascending, contiguous, starts at the reference year
            var map = new Dictionary<int, int>(years.Count);

            int cumulative = 0;
            foreach (var year in years)
            {
                map[year.Year] = cumulative;
                cumulative += year.MonthDays.Sum();
            }

            _yearStartOffset = map;
        }

        /// <summary>
        /// Guards against the reference anchor and the data file silently drifting apart. The
        /// day-count walk starts from <see cref="_referenceBsDate"/>; if the data no longer starts
        /// at that same year, every conversion would be wrong. Fails loudly instead.
        /// </summary>
        private void VerifyReferenceMatchesData()
        {
            if (_referenceVerified)
                return;

            int firstDataYear = _dataService.GetYears()[0].Year;
            if (firstDataYear != _referenceBsDate.Year)
            {
                throw new InvalidOperationException(
                    $"Calendar data starts at BS {firstDataYear} but the converter reference year is BS " +
                    $"{_referenceBsDate.Year}. They must match or all conversions will be off.");
            }

            _referenceVerified = true;
        }

        public BsDate ConvertFromAd(DateTime adDate)
        {
            EnsureIndex();

            if (adDate < _referenceAdDate)
                throw new NotSupportedException("Dates before the reference date are not supported yet.");

            int offset = (adDate - _referenceAdDate).Days;

            foreach (var yearData in _dataService.GetYears())
            {
                int start = _yearStartOffset![yearData.Year];
                int total = yearData.MonthDays.Sum();

                if (offset < start || offset >= start + total)
                    continue;

                int remaining = offset - start;
                int month = 1;
                while (remaining >= yearData.MonthDays[month - 1])
                {
                    remaining -= yearData.MonthDays[month - 1];
                    month++;
                }

                return new BsDate
                {
                    Year = yearData.Year,
                    Month = month,
                    Day = remaining + 1,
                    DayName = adDate.DayOfWeek.ToString()
                };
            }

            throw new Exception($"Missing BS data for the date {adDate:yyyy-MM-dd}.");
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
            EnsureIndex();

            if (bsYear < _referenceBsDate.Year)
                throw new NotSupportedException("BS years before the reference year are not supported yet.");

            if (!_yearStartOffset!.TryGetValue(bsYear, out int totalOffset))
                throw new Exception($"Missing BS data for year {bsYear}.");

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