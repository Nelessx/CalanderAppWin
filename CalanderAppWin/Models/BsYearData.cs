namespace NepaliCalendar.App.Models
{
    public class BsYearData
    {
        public int Year { get; set; }
        public int[] MonthDays { get; set; } = new int[12];
    }
}