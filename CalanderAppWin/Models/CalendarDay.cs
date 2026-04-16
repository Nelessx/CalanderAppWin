namespace NepaliCalendar.App.Models
{
    public class CalendarDay
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int Day { get; set; }
        public string DayName { get; set; } = "";
        public bool IsToday { get; set; }

        public string DisplayText => Day.ToString();
    }
}