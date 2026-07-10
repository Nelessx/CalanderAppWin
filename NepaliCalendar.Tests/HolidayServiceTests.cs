using System;
using System.Linq;
using NepaliCalendar.App.Services;
using Xunit;

namespace NepaliCalendar.Tests
{
    public class HolidayServiceTests
    {
        private readonly HolidayService _service = new();

        [Fact]
        public void NewYear_FallsOnBaisakh1_EveryYear()
        {
            foreach (int year in new[] { 2082, 2083, 2084 })
            {
                var day = _service.GetForBsDate(year, 1, 1);
                Assert.Contains(day, h => h.Title == "Nepali New Year");
            }
        }

        [Fact]
        public void RecurringHolidays_ExpandToTheRequestedYear()
        {
            var y2083 = _service.GetHolidaysForBsYear(2083);
            Assert.All(y2083, h => Assert.Equal(2083, h.BsYear));
            Assert.Contains(y2083, h => h.Title == "Constitution Day" && h.BsMonth == 6 && h.BsDay == 3);
            Assert.Contains(y2083, h => h.Title == "Republic Day" && h.BsMonth == 2 && h.BsDay == 15);
        }

        [Fact]
        public void Holidays_AreMarkedAllDayAndPublic()
        {
            var newYear = _service.GetForBsDate(2083, 1, 1).First(h => h.Title == "Nepali New Year");
            Assert.True(newYear.IsHoliday);
            Assert.True(newYear.IsAllDay);
            Assert.True(newYear.IsPublicHoliday);
            Assert.Equal("Public Holiday", newYear.BadgeText);
        }

        [Fact]
        public void GetUpcoming_ReturnsHolidaysOnOrAfterTheDate_Sorted()
        {
            var upcoming = _service.GetUpcoming(2083, new DateTime(2026, 4, 13), 3);
            Assert.NotEmpty(upcoming);
            Assert.True(upcoming.SequenceEqual(upcoming.OrderBy(h => h.AdDate)));
            Assert.All(upcoming, h => Assert.True(h.AdDate.Date >= new DateTime(2026, 4, 13)));
        }

        [Fact]
        public void MovableFestival_VijayaDashami_LandsOnItsPublishedDate()
        {
            // Vijaya Dashami BS 2081 = Ashwin 27 (AD 2024-10-13); BS 2082 = Ashwin 16 (AD 2025-10-02).
            var d2081 = _service.GetForBsDate(2081, 6, 27);
            Assert.Contains(d2081, h => h.Title == "Vijaya Dashami" && h.AdDate == new DateTime(2024, 10, 13));

            var d2082 = _service.GetForBsDate(2082, 6, 16);
            Assert.Contains(d2082, h => h.Title == "Vijaya Dashami" && h.AdDate == new DateTime(2025, 10, 2));
        }

        [Fact]
        public void DashainDays_ShareFestivalGrouping()
        {
            var dashain = _service.GetHolidaysForBsYear(2081)
                .Where(h => h.EventType == "Dashain")
                .ToList();

            Assert.Contains(dashain, h => h.Title == "Ghatasthapana");
            Assert.Contains(dashain, h => h.Title == "Vijaya Dashami");
            Assert.True(dashain.Count >= 5, "Dashain should span several days");
        }

        [Fact]
        public void MovableFestivals_PopulateEachSupportedYear()
        {
            // Each fully-sourced year should carry a substantial festival set, not just the fixed ones.
            foreach (int year in new[] { 2081, 2082, 2083, 2084 })
            {
                var holidays = _service.GetHolidaysForBsYear(year);
                Assert.True(holidays.Count >= 25, $"BS {year} should have a full holiday set, had {holidays.Count}");
                Assert.Contains(holidays, h => h.Title == "Bhai Tika");
                Assert.Contains(holidays, h => h.Title == "Maha Shivaratri");
            }
        }

        [Fact]
        public void RegionalFestival_CarriesRegionTag()
        {
            var indraJatra = _service.GetHolidaysForBsYear(2081).FirstOrDefault(h => h.Title == "Indra Jatra");
            Assert.NotNull(indraJatra);
            Assert.Equal("Kathmandu Valley", indraJatra!.Region);
        }
    }
}
