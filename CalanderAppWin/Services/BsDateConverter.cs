using System;
using NepaliCalendar.App.Models;

namespace NepaliCalendar.App.Services
{
    public class BsDateConverter
    {
        public BsDate ConvertFromAd(DateTime adDate)
        {
            return new BsDate
            {
                Year = 2081,
                Month = 1,
                Day = 1,
                DayName = adDate.DayOfWeek.ToString()
            };
        }
    }
}