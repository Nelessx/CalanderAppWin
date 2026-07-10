using System;
using NepaliCalendar.App.Models;
using NepaliCalendar.App.Services;
using Xunit;

namespace NepaliCalendar.Tests
{
    public class ReminderServiceTests
    {
        [Fact]
        public void NoReminder_YieldsNull()
        {
            var e = new CalendarEvent { AdDate = new DateTime(2026, 5, 10), IsAllDay = true, ReminderMinutesBefore = null };
            Assert.Null(ReminderService.GetReminderTime(e));
        }

        [Fact]
        public void TimedEvent_SubtractsMinutesFromStart()
        {
            var e = new CalendarEvent
            {
                AdDate = new DateTime(2026, 5, 10),
                IsAllDay = false,
                TimeText = "09:30 AM",
                ReminderMinutesBefore = 15
            };

            Assert.Equal(new DateTime(2026, 5, 10, 9, 15, 0), ReminderService.GetReminderTime(e));
        }

        [Fact]
        public void AllDayEvent_AnchorsToNineAm()
        {
            var e = new CalendarEvent
            {
                AdDate = new DateTime(2026, 5, 10),
                IsAllDay = true,
                ReminderMinutesBefore = 0
            };

            Assert.Equal(new DateTime(2026, 5, 10, 9, 0, 0), ReminderService.GetReminderTime(e));
        }

        [Fact]
        public void AllDayEvent_OneDayBefore()
        {
            var e = new CalendarEvent
            {
                AdDate = new DateTime(2026, 5, 10),
                IsAllDay = true,
                ReminderMinutesBefore = 1440
            };

            Assert.Equal(new DateTime(2026, 5, 9, 9, 0, 0), ReminderService.GetReminderTime(e));
        }

        [Fact]
        public void UnparseableTime_FallsBackToAllDayAnchor()
        {
            var e = new CalendarEvent
            {
                AdDate = new DateTime(2026, 5, 10),
                IsAllDay = false,
                TimeText = "sometime",
                ReminderMinutesBefore = 30
            };

            Assert.Equal(new DateTime(2026, 5, 10, 8, 30, 0), ReminderService.GetReminderTime(e));
        }
    }
}
