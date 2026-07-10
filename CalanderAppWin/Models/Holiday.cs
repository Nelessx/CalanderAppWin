namespace NepaliCalendar.App.Models
{
    /// <summary>
    /// A Nepali holiday. When <see cref="BsYear"/> is null the holiday recurs on the
    /// same BS month/day every year (true for fixed-date national holidays). Movable
    /// festivals should be entered with an explicit BsYear per occurrence.
    /// </summary>
    public class Holiday
    {
        public string Title { get; set; } = string.Empty;
        public string? NepaliTitle { get; set; }

        public int? BsYear { get; set; }
        public int BsMonth { get; set; }
        public int BsDay { get; set; }

        public bool IsPublic { get; set; } = true;
    }
}
