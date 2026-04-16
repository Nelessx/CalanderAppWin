namespace NepaliCalendar.App.Models
{
    public class CalendarCell
    {
        public string Text { get; set; } = "";
        public string DayName { get; set; } = "";
        public bool IsToday { get; set; }
        public bool IsCurrentMonth { get; set; } = true;
        public int DayOfWeekIndex { get; set; }

        public int Year { get; set; }
        public int Month { get; set; }
        public int Day { get; set; }
        public bool IsSelected { get; set; }
    }
}