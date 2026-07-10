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
    }
}
