using NepaliCalendar.App.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NepaliCalendar.App.Services
{
    public class DashboardMockDataService
    {
        private readonly BsDateConverter _converter = new();

        public List<CalendarEvent> GetUpcomingEvents()
        {
            var eventDate1 = DateTime.Today;
            var eventDate2 = DateTime.Today.AddDays(1);
            var eventDate3 = DateTime.Today.AddDays(2);

            var bsDate1 = _converter.ConvertFromAd(eventDate1);
            var bsDate2 = _converter.ConvertFromAd(eventDate2);
            var bsDate3 = _converter.ConvertFromAd(eventDate3);

            return new List<CalendarEvent>
            {
                new CalendarEvent
                {
                    Title = "Team Standup",
                    NepaliTitle = "टोली बैठक",
                    AdDate = eventDate1,
                    BsYear = bsDate1.Year,
                    BsMonth = bsDate1.Month,
                    BsDay = bsDate1.Day,
                    EventType = "Meeting",
                    BadgeText = "Work",
                    IsAllDay = false,
                    TimeText = "09:30 AM",
                    DayText = eventDate1.DayOfWeek.ToString()
                },
                new CalendarEvent
                {
                    Title = "Project Demo",
                    NepaliTitle = "परियोजना प्रस्तुति",
                    AdDate = eventDate2,
                    BsYear = bsDate2.Year,
                    BsMonth = bsDate2.Month,
                    BsDay = bsDate2.Day,
                    EventType = "Presentation",
                    BadgeText = "Office",
                    IsAllDay = false,
                    TimeText = "02:00 PM",
                    DayText = eventDate2.DayOfWeek.ToString()
                },
                new CalendarEvent
                {
                    Title = "Family Dinner",
                    NepaliTitle = "पारिवारिक भेट",
                    AdDate = eventDate3,
                    BsYear = bsDate3.Year,
                    BsMonth = bsDate3.Month,
                    BsDay = bsDate3.Day,
                    EventType = "Personal",
                    BadgeText = "Personal",
                    IsAllDay = false,
                    TimeText = "07:00 PM",
                    DayText = eventDate3.DayOfWeek.ToString()
                }
            };
        }

        public List<CalendarEvent> GetHolidays()
        {
            var holidayDate1 = DateTime.Today.AddDays(3);
            var holidayDate2 = DateTime.Today.AddDays(10);

            var bsDate1 = _converter.ConvertFromAd(holidayDate1);
            var bsDate2 = _converter.ConvertFromAd(holidayDate2);

            return new List<CalendarEvent>
            {
                new CalendarEvent
                {
                    Title = "Constitution Day",
                    NepaliTitle = "संविधान दिवस",
                    AdDate = holidayDate1,
                    BsYear = bsDate1.Year,
                    BsMonth = bsDate1.Month,
                    BsDay = bsDate1.Day,
                    EventType = "Holiday",
                    BadgeText = "Public Holiday",
                    IsHoliday = true,
                    IsPublicHoliday = true,
                    IsAllDay = true,
                    TimeText = "All Day",
                    DayText = holidayDate1.DayOfWeek.ToString()
                },
                new CalendarEvent
                {
                    Title = "Buddha Jayanti",
                    NepaliTitle = "बुद्ध जयन्ती",
                    AdDate = holidayDate2,
                    BsYear = bsDate2.Year,
                    BsMonth = bsDate2.Month,
                    BsDay = bsDate2.Day,
                    EventType = "Holiday",
                    BadgeText = "Holiday",
                    IsHoliday = true,
                    IsPublicHoliday = false,
                    IsAllDay = true,
                    TimeText = "All Day",
                    DayText = holidayDate2.DayOfWeek.ToString()
                }
            };
        }

        public List<QuickActionItem> GetQuickActions()
        {
            return new List<QuickActionItem>
            {
                new QuickActionItem
                {
                    Title = "Add Event",
                    IconGlyph = "+",
                    ActionKey = "add_event"
                },
                new QuickActionItem
                {
                    Title = "View Events",
                    IconGlyph = "•",
                    ActionKey = "view_events"
                },
                new QuickActionItem
                {
                    Title = "Converter",
                    IconGlyph = "⇄",
                    ActionKey = "converter"
                },
                new QuickActionItem
                {
                    Title = "Export Calendar",
                    IconGlyph = "↓",
                    ActionKey = "export_calendar"
                }
            };
        }

        public List<DashboardSectionItem> GetUpcomingEventCards()
        {
            return GetUpcomingEvents()
                .Select(e => new DashboardSectionItem
                {
                    Title = e.Title,
                    Subtitle = e.BadgeText,
                    SecondaryText = e.AdDate.ToString("MMMM d, yyyy"),
                    TertiaryText = e.IsAllDay ? "All Day" : e.TimeText,
                    BadgeText = e.BadgeText,
                    ShowBadge = !string.IsNullOrWhiteSpace(e.BadgeText),
                    IsEvent = true,
                    IsHoliday = false
                })
                .ToList();
        }

        public List<DashboardSectionItem> GetHolidayCards()
        {
            return GetHolidays()
                .Select(h => new DashboardSectionItem
                {
                    Title = h.Title,
                    Subtitle = h.NepaliTitle,
                    SecondaryText = h.AdDate.ToString("MMMM d, yyyy"),
                    TertiaryText = h.DayText,
                    BadgeText = h.BadgeText,
                    ShowBadge = !string.IsNullOrWhiteSpace(h.BadgeText),
                    IsEvent = false,
                    IsHoliday = true
                })
                .ToList();
        }
    }
}