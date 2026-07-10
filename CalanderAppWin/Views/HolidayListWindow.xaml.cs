using System.Linq;
using System.Windows;
using NepaliCalendar.App.Models;
using NepaliCalendar.App.Services;

namespace NepaliCalendar.App.Views
{
    /// <summary>
    /// Read-only list of every Nepali holiday across the supported BS years. Holidays come from
    /// the bundled data set (not user-editable), so unlike the event list this window only shows
    /// them — localized to the current language.
    /// </summary>
    public partial class HolidayListWindow : Window
    {
        private readonly HolidayService _holidayService = new();
        private readonly LocalizationService _localization = new();
        private readonly NepaliNumberService _nepaliNumbers = new();

        public HolidayListWindow(AppLanguage language)
        {
            InitializeComponent();

            _localization.CurrentLanguage = language;
            ApplyLocalizedChrome();
            LoadHolidays();
        }

        private void ApplyLocalizedChrome()
        {
            Title = _localization.GetHolidaysWindowTitle();
            HeaderText.Text = _localization.GetHolidaysWindowTitle();
            CloseButton.Content = _localization.GetCloseText();
            EmptyState.Text = _localization.GetNoHolidaysInRangeText();
        }

        private void LoadHolidays()
        {
            bool useNepaliNumbers = _localization.CurrentLanguage == AppLanguage.Nepali;

            var items = _holidayService.GetAllAcrossSupportedYears()
                .Select(h =>
                {
                    string monthName = _localization.GetMonthName(h.BsMonth);
                    string day = useNepaliNumbers ? _nepaliNumbers.ToNepaliNumber(h.BsDay) : h.BsDay.ToString();
                    string year = useNepaliNumbers ? _nepaliNumbers.ToNepaliNumber(h.BsYear) : h.BsYear.ToString();
                    string? subtitle = _localization.GetHolidayAltTitle(h.Title, h.NepaliTitle);

                    return new HolidayListItem
                    {
                        Title = _localization.GetHolidayDisplayTitle(h.Title, h.NepaliTitle),
                        Subtitle = subtitle,
                        ShowSubtitle = !string.IsNullOrWhiteSpace(subtitle),
                        DateText = $"{monthName} {day}, {year}  ·  {h.AdDate:MMM d, yyyy}",
                        WeekdayText = _localization.GetWeekdayName(h.AdDate.DayOfWeek),
                        BadgeText = _localization.GetHolidayBadgeText(h.IsPublicHoliday)
                    };
                })
                .ToList();

            HolidaysItemsControl.ItemsSource = items;
            HolidayCountText.Text = useNepaliNumbers
                ? _nepaliNumbers.ToNepaliNumber(items.Count)
                : items.Count.ToString();
            EmptyState.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        public sealed class HolidayListItem
        {
            public string Title { get; init; } = string.Empty;
            public string? Subtitle { get; init; }
            public bool ShowSubtitle { get; init; }
            public string DateText { get; init; } = string.Empty;
            public string WeekdayText { get; init; } = string.Empty;
            public string BadgeText { get; init; } = string.Empty;
        }
    }
}
