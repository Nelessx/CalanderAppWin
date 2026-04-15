using System.Collections.Generic;
using NepaliCalendar.App.Models;

namespace NepaliCalendar.App.Data
{
    public static class BsCalendarData
    {
        public static List<BsYearData> Years => new()
        {
            new BsYearData
            {
                Year = 2081,
                MonthDays = new[] { 31, 31, 32, 32, 31, 30, 30, 30, 29, 29, 30, 31 }
            },
            new BsYearData
            {
                Year = 2082,
                MonthDays = new[] { 30, 32, 31, 32, 31, 30, 30, 30, 29, 30, 29, 31 }
            }
        };
    }
}