namespace NepaliCalendar.App.Models
{
    public class DashboardSectionItem
    {
        public string Title { get; set; } = string.Empty;
        public string? Subtitle { get; set; }
        public string? SecondaryText { get; set; }
        public string? TertiaryText { get; set; }

        public string? BadgeText { get; set; }
        public bool ShowBadge { get; set; }

        public bool IsHoliday { get; set; }
        public bool IsEvent { get; set; }
    }
}