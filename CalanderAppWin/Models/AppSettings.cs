namespace NepaliCalendar.App.Models
{
    public class AppSettings
    {
        public AppLanguage Language { get; set; } = AppLanguage.English;
        public WidgetSize SelectedWidgetSize { get; set; } = WidgetSize.Large;

        public double SmallWidgetLeft { get; set; }
        public double SmallWidgetTop { get; set; }
        public bool HasSavedSmallWidgetPosition { get; set; } = false;

        public double MediumWidgetLeft { get; set; }
        public double MediumWidgetTop { get; set; }
        public bool HasSavedMediumWidgetPosition { get; set; } = false;

        public double LargeWidgetLeft { get; set; }
        public double LargeWidgetTop { get; set; }
        public bool HasSavedLargeWidgetPosition { get; set; } = false;
    }
}