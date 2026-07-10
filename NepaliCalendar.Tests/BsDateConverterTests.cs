using System;
using NepaliCalendar.App.Services;
using Xunit;

namespace NepaliCalendar.Tests
{
    public class BsDateConverterTests
    {
        private readonly BsDateConverter _c = new();

        [Fact]
        public void ReferenceDate_ConvertsBothWays()
        {
            Assert.Equal(new DateTime(2024, 4, 13), _c.ConvertToAd(2081, 1, 1));

            var bs = _c.ConvertFromAd(new DateTime(2024, 4, 13));
            Assert.Equal((2081, 1, 1), (bs.Year, bs.Month, bs.Day));
        }

        [Theory]
        [InlineData("2024-04-13")]
        [InlineData("2026-05-07")]
        [InlineData("2028-04-01")]
        [InlineData("2030-12-31")]
        public void RoundTripsExactly(string adText)
        {
            var ad = DateTime.Parse(adText);
            var bs = _c.ConvertFromAd(ad);
            Assert.Equal(ad, _c.ConvertToAd(bs.Year, bs.Month, bs.Day));
        }

        [Fact]
        public void DataCoversExpectedYears()
        {
            var years = _c.GetAvailableYears();
            Assert.Equal(2081, years[0]);
            Assert.Equal(2087, years[^1]);
            Assert.Equal(7, years.Count);
        }

        [Fact]
        public void OutOfRange_DoesNotThrow()
        {
            Assert.False(_c.TryConvertFromAd(new DateTime(2020, 1, 1), out _));
            Assert.False(_c.TryConvertFromAd(new DateTime(2050, 1, 1), out _));
            Assert.True(_c.TryConvertFromAd(new DateTime(2026, 5, 7), out var bs));
            Assert.NotNull(bs);
        }

        [Fact]
        public void MonthGrid_HasCorrectLeadingBlanks()
        {
            // BS 2081-01-01 == AD 2024-04-13 == Saturday (DayOfWeek 6).
            var grid = _c.GetMonthGrid(2081, 1, useNepaliNumbers: false);
            int firstReal = grid.FindIndex(cell => cell.IsCurrentMonth);

            Assert.Equal(6, firstReal);
            Assert.Equal(1, grid[firstReal].Day);
            Assert.Equal(0, grid.Count % 7);
        }

        [Fact]
        public void Weekday_AgreesWithGregorian()
        {
            var probe = new DateTime(2026, 5, 7);
            Assert.Equal(probe.DayOfWeek.ToString(), _c.ConvertFromAd(probe).DayName);
        }
    }
}
