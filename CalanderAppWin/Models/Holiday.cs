namespace NepaliCalendar.App.Models
{
    /// <summary>
    /// A Nepali holiday. When <see cref="BsYear"/> is null the holiday recurs on the same BS
    /// month/day every year (true for national holidays with an official fixed BS date). Movable
    /// lunar festivals — whose date is set anew each year — are entered with an explicit BsYear
    /// per occurrence.
    /// </summary>
    public class Holiday
    {
        public string Title { get; set; } = string.Empty;
        public string? NepaliTitle { get; set; }

        public int? BsYear { get; set; }
        public int BsMonth { get; set; }
        public int BsDay { get; set; }

        public bool IsPublic { get; set; } = true;

        /// <summary>National, Religious, Cultural, or Observance — drives grouping and colour.</summary>
        public string? Category { get; set; }

        /// <summary>Non-null when a holiday is observed only in a region, e.g. "Kathmandu Valley".</summary>
        public string? Region { get; set; }

        /// <summary>Groups multi-day festivals (e.g. "Dashain", "Tihar") whose days are separate rows.</summary>
        public string? Festival { get; set; }
    }

    /// <summary>
    /// On-disk shape of Data/holidays.json: a small provenance header plus the holiday rows.
    /// Kept separate from <see cref="Holiday"/> so the data file can carry a source/version.
    /// </summary>
    public class HolidayFile
    {
        public string? Source { get; set; }
        public string? Note { get; set; }
        public string? Version { get; set; }
        public System.Collections.Generic.List<Holiday> Holidays { get; set; } = new();
    }
}
