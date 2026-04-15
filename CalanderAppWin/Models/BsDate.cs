namespace NepaliCalendar.App.Models
{
    public class BsDate
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int Day { get; set; }
        public string DayName { get; set; } = "";

        public override string ToString()
        {
            return $"{Year}/{Month:D2}/{Day:D2}";
        }
    }
}