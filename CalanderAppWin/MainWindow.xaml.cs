using NepaliCalendar.App.Models;
using NepaliCalendar.App.Services;
using NepaliCalendar.App.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NepaliCalendar.App
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private readonly BsDateConverter _converter = new();
        private readonly NepaliNumberService _nepaliNumberService = new();
        private readonly LocalizationService _localizationService = new();
        private readonly SettingsService _settingsService = new();
        private readonly DashboardMockDataService _dashboardMockDataService = new();
        private readonly EventStore _eventStore = new();
        private readonly CalendarExportService _exportService = new();

        private const int UpcomingEventsLimit = 5;

        private int _currentYear;
        private int _currentMonth;
        private int _selectedYear;
        private int _selectedMonth;
        private int _selectedDay;
        private bool _hasSelectedDate;
        private bool _isUpdatingSelectors;

        public event PropertyChangedEventHandler? PropertyChanged;

        public List<DashboardSectionItem> UpcomingEventCards { get; private set; } = new();
        public List<DashboardSectionItem> HolidayCards { get; private set; } = new();
        public List<QuickActionItem> QuickActions { get; private set; } = new();

        public List<DashboardSectionItem> SelectedDateEventCards { get; private set; } = new();
        public List<DashboardSectionItem> SelectedDateHolidayCards { get; private set; } = new();

        public bool HasSelectedDateEvents => SelectedDateEventCards.Count > 0;
        public bool HasSelectedDateHolidays => SelectedDateHolidayCards.Count > 0;
        public bool HasNoSelectedDateEvents => SelectedDateEventCards.Count == 0;
        public bool HasNoSelectedDateHolidays => SelectedDateHolidayCards.Count == 0;

        public int UpcomingEventsCount => UpcomingEventCards.Count;
        public int HolidayCount => HolidayCards.Count;
        public int SelectedDateEventsCount => SelectedDateEventCards.Count;
        public int SelectedDateHolidaysCount => SelectedDateHolidayCards.Count;

        public bool IsSelectedDateToday
        {
            get
            {
                if (!_hasSelectedDate)
                    return false;

                if (!_converter.TryConvertFromAd(DateTime.Today, out var todayBs) || todayBs is null)
                    return false;

                return _selectedYear == todayBs.Year
                    && _selectedMonth == todayBs.Month
                    && _selectedDay == todayBs.Day;
            }
        }

        public string SelectedSidebarDateText
        {
            get
            {
                if (!_hasSelectedDate)
                    return "No date selected";

                bool useNepaliNumbers = _localizationService.CurrentLanguage == AppLanguage.Nepali;

                string monthName = _localizationService.GetMonthName(_selectedMonth);
                string dayText = useNepaliNumbers
                    ? _nepaliNumberService.ToNepaliNumber(_selectedDay)
                    : _selectedDay.ToString();

                string yearText = useNepaliNumbers
                    ? _nepaliNumberService.ToNepaliNumber(_selectedYear)
                    : _selectedYear.ToString();

                return $"{monthName} {dayText}, {yearText}";
            }
        }

        public string FooterSelectedDateText
        {
            get
            {
                if (!_hasSelectedDate)
                    return "No selected date";

                return SelectedSidebarDateText;
            }
        }

        public string FooterLanguageText =>
            _localizationService.CurrentLanguage == AppLanguage.Nepali
                ? "Language: नेपाली"
                : "Language: English";

        public string FooterStatusText =>
            IsSelectedDateToday
                ? "Selected date is today"
                : "Dashboard ready";

        public MainWindow()
        {
            InitializeComponent();
            Closed += MainWindow_Closed;
            DataContext = this;

#if !DEBUG
            GenerateJsonButton.Visibility = Visibility.Collapsed;
#endif

            try
            {
                var settings = _settingsService.Load();
                _localizationService.CurrentLanguage = settings.Language;

                var todayBs = _converter.ConvertFromAd(DateTime.Today);
                _currentYear = todayBs.Year;
                _currentMonth = todayBs.Month;
                _selectedYear = todayBs.Year;
                _selectedMonth = todayBs.Month;
                _selectedDay = todayBs.Day;
                _hasSelectedDate = true;

                PopulateLanguageDropdown();
                PopulateYearDropdown();
                PopulateMonthDropdown();
                ApplyLocalizedText();
                LoadDashboardData();
                LoadCalendar();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Calendar Data Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                BsDateText.Text = "Failed to load calendar";
                SelectedBsDateText.Text = "Failed to load calendar";
                SelectedBsDayText.Text = ex.Message;
            }
        }

        private void ApplyLocalizedText()
        {
            Title = _localizationService.CurrentLanguage == AppLanguage.Nepali
                ? "नेपाली पात्रो"
                : "Nepali Calendar";

            PreviousButton.Content = _localizationService.GetPreviousText();
            TodayButton.Content = _localizationService.GetTodayText();
            NextButton.Content = _localizationService.GetNextText();

            LanguageLabelTextBlock.Text = _localizationService.GetLanguageLabelText();
            MonthLabelTextBlock.Text = _localizationService.GetMonthLabelText();
            YearLabelTextBlock.Text = _localizationService.GetYearLabelText();

            var headers = _localizationService.GetWeekdayHeaders();
            SunHeader.Text = headers[0];
            MonHeader.Text = headers[1];
            TueHeader.Text = headers[2];
            WedHeader.Text = headers[3];
            ThuHeader.Text = headers[4];
            FriHeader.Text = headers[5];
            SatHeader.Text = headers[6];
        }

        private void LoadDashboardData()
        {
            UpcomingEventCards = _eventStore
                .GetUpcoming(DateTime.Today, UpcomingEventsLimit)
                .ConvertAll(MapEventToCard);

            HolidayCards = _dashboardMockDataService.GetHolidayCards();
            QuickActions = _dashboardMockDataService.GetQuickActions();

            LoadSelectedDateDashboardData();
            RefreshDashboardBindings();
        }

        private static DashboardSectionItem MapEventToCard(CalendarEvent calendarEvent)
        {
            return new DashboardSectionItem
            {
                Title = calendarEvent.Title,
                Subtitle = calendarEvent.BadgeText,
                SecondaryText = calendarEvent.AdDate.ToString("MMMM d, yyyy"),
                TertiaryText = calendarEvent.IsAllDay ? "All Day" : calendarEvent.TimeText,
                BadgeText = calendarEvent.BadgeText,
                ShowBadge = !string.IsNullOrWhiteSpace(calendarEvent.BadgeText),
                IsEvent = true,
                IsHoliday = false
            };
        }

        private void LoadSelectedDateDashboardData()
        {
            if (!_hasSelectedDate)
            {
                SelectedDateEventCards = new List<DashboardSectionItem>();
                SelectedDateHolidayCards = new List<DashboardSectionItem>();
                return;
            }

            var selectedEvents = _eventStore.GetForBsDate(_selectedYear, _selectedMonth, _selectedDay);

            var selectedHolidays = _dashboardMockDataService.GetHolidays()
                .FindAll(h =>
                    h.BsYear == _selectedYear &&
                    h.BsMonth == _selectedMonth &&
                    h.BsDay == _selectedDay);

            SelectedDateEventCards = selectedEvents.ConvertAll(MapEventToCard);

            SelectedDateHolidayCards = selectedHolidays
                .ConvertAll(h => new DashboardSectionItem
                {
                    Title = h.Title,
                    Subtitle = h.NepaliTitle,
                    SecondaryText = h.AdDate.ToString("MMMM d, yyyy"),
                    TertiaryText = h.DayText,
                    BadgeText = h.BadgeText,
                    ShowBadge = !string.IsNullOrWhiteSpace(h.BadgeText),
                    IsEvent = false,
                    IsHoliday = true
                });
        }

        private void RefreshDashboardBindings()
        {
            OnPropertyChanged(nameof(UpcomingEventCards));
            OnPropertyChanged(nameof(HolidayCards));
            OnPropertyChanged(nameof(QuickActions));

            OnPropertyChanged(nameof(SelectedDateEventCards));
            OnPropertyChanged(nameof(SelectedDateHolidayCards));

            OnPropertyChanged(nameof(HasSelectedDateEvents));
            OnPropertyChanged(nameof(HasSelectedDateHolidays));
            OnPropertyChanged(nameof(HasNoSelectedDateEvents));
            OnPropertyChanged(nameof(HasNoSelectedDateHolidays));

            OnPropertyChanged(nameof(UpcomingEventsCount));
            OnPropertyChanged(nameof(HolidayCount));
            OnPropertyChanged(nameof(SelectedDateEventsCount));
            OnPropertyChanged(nameof(SelectedDateHolidaysCount));

            OnPropertyChanged(nameof(IsSelectedDateToday));
            OnPropertyChanged(nameof(SelectedSidebarDateText));
            OnPropertyChanged(nameof(FooterSelectedDateText));
            OnPropertyChanged(nameof(FooterLanguageText));
            OnPropertyChanged(nameof(FooterStatusText));
        }

        private void ApplyCalendarIndicators(List<CalendarCell> grid)
        {
            var events = _eventStore.GetAll();
            var holidays = _dashboardMockDataService.GetHolidays();

            foreach (var cell in grid)
            {
                if (!cell.IsCurrentMonth || cell.Day <= 0)
                {
                    cell.HasEvent = false;
                    cell.HasHoliday = false;
                    continue;
                }

                cell.HasEvent = events.Exists(e =>
                    e.BsYear == cell.Year &&
                    e.BsMonth == cell.Month &&
                    e.BsDay == cell.Day);

                cell.HasHoliday = holidays.Exists(h =>
                    h.BsYear == cell.Year &&
                    h.BsMonth == cell.Month &&
                    h.BsDay == cell.Day);
            }
        }

        private void UpdateSelectedDateInfo(int totalDaysInMonth)
        {
            if (!_hasSelectedDate)
                return;

            bool useNepaliNumbers = _localizationService.CurrentLanguage == AppLanguage.Nepali;

            string selectedBsMonthName = _localizationService.GetMonthName(_selectedMonth);
            string selectedBsDay = useNepaliNumbers
                ? _nepaliNumberService.ToNepaliNumber(_selectedDay)
                : _selectedDay.ToString();

            string selectedBsYear = useNepaliNumbers
                ? _nepaliNumberService.ToNepaliNumber(_selectedYear)
                : _selectedYear.ToString();

            DateTime selectedAdDate = _converter.ConvertToAd(_selectedYear, _selectedMonth, _selectedDay);

            string adDateText = _localizationService.CurrentLanguage == AppLanguage.Nepali
                ? $"{_nepaliNumberService.ToNepaliNumber(selectedAdDate.Day)} {selectedAdDate:MMMM} {_nepaliNumberService.ToNepaliNumber(selectedAdDate.Year)}"
                : $"{selectedAdDate:MMMM d, yyyy}";

            string bsDayName = _localizationService.CurrentLanguage == AppLanguage.Nepali
                ? GetNepaliDayName(selectedAdDate.DayOfWeek)
                : selectedAdDate.DayOfWeek.ToString();

            string adDayName = _localizationService.CurrentLanguage == AppLanguage.Nepali
                ? GetNepaliDayName(selectedAdDate.DayOfWeek)
                : selectedAdDate.DayOfWeek.ToString();

            SelectedBsDateText.Text = $"{selectedBsMonthName} {selectedBsDay}, {selectedBsYear}";
            SelectedBsDayText.Text = bsDayName;
            SelectedAdDateText.Text = adDateText;
            SelectedAdDayText.Text = adDayName;
            TotalDaysInfoText.Text = useNepaliNumbers
                ? _nepaliNumberService.ToNepaliNumber(totalDaysInMonth)
                : totalDaysInMonth.ToString();
        }

        private string GetNepaliDayName(DayOfWeek dayOfWeek)
        {
            return dayOfWeek switch
            {
                DayOfWeek.Sunday => "आइतबार",
                DayOfWeek.Monday => "सोमबार",
                DayOfWeek.Tuesday => "मंगलबार",
                DayOfWeek.Wednesday => "बुधबार",
                DayOfWeek.Thursday => "बिहिबार",
                DayOfWeek.Friday => "शुक्रबार",
                DayOfWeek.Saturday => "शनिबार",
                _ => ""
            };
        }

        private void LoadCalendar()
        {
            var monthDays = _converter.GetMonthDays(_currentYear, _currentMonth);

            bool useNepaliNumbers = _localizationService.CurrentLanguage == AppLanguage.Nepali;
            var grid = _converter.GetMonthGrid(_currentYear, _currentMonth, useNepaliNumbers);

            foreach (var cell in grid)
            {
                cell.IsSelected = cell.IsCurrentMonth
                    && _hasSelectedDate
                    && cell.Year == _selectedYear
                    && cell.Month == _selectedMonth
                    && cell.Day == _selectedDay;
            }

            ApplyCalendarIndicators(grid);

            string monthName = _localizationService.GetMonthName(_currentMonth);
            string yearText = useNepaliNumbers
                ? _nepaliNumberService.ToNepaliNumber(_currentYear)
                : _currentYear.ToString();

            BsDateText.Text = $"{monthName} {yearText}";
            UpdateSelectedDateInfo(monthDays.Count);
            CalendarGrid.ItemsSource = grid;

            SetSelectedDropdownValues();
            UpdateNavigationButtonStates();
            RefreshDashboardBindings();
        }

        private void UpdateNavigationButtonStates()
        {
            var availableYears = _converter.GetAvailableYears();
            int firstYear = availableYears[0];
            int lastYear = availableYears[^1];

            bool isAtFirstMonth = _currentYear == firstYear && _currentMonth == 1;
            bool isAtLastMonth = _currentYear == lastYear && _currentMonth == 12;

            PreviousButton.IsEnabled = !isAtFirstMonth;
            NextButton.IsEnabled = !isAtLastMonth;
        }

        private void SaveLanguageSetting(AppLanguage language)
        {
            var settings = _settingsService.Load();
            settings.Language = language;
            _settingsService.Save(settings);
        }

        private void PopulateLanguageDropdown()
        {
            _isUpdatingSelectors = true;

            LanguageComboBox.ItemsSource = new List<LanguageDropdownItem>
            {
                new LanguageDropdownItem
                {
                    Value = AppLanguage.English,
                    DisplayText = _localizationService.GetLanguageDisplayText(AppLanguage.English)
                },
                new LanguageDropdownItem
                {
                    Value = AppLanguage.Nepali,
                    DisplayText = _localizationService.GetLanguageDisplayText(AppLanguage.Nepali)
                }
            };

            LanguageComboBox.DisplayMemberPath = "DisplayText";
            LanguageComboBox.SelectedValuePath = "Value";
            LanguageComboBox.SelectedValue = _localizationService.CurrentLanguage;

            _isUpdatingSelectors = false;
        }

        private void PopulateMonthDropdown()
        {
            _isUpdatingSelectors = true;

            var items = new List<DropdownItem>();
            bool useNepaliNumbers = _localizationService.CurrentLanguage == AppLanguage.Nepali;

            for (int month = 1; month <= 12; month++)
            {
                string monthName = _localizationService.GetMonthName(month);
                string monthNumber = useNepaliNumbers
                    ? _nepaliNumberService.ToNepaliNumber(month)
                    : month.ToString();

                items.Add(new DropdownItem
                {
                    Value = month,
                    DisplayText = $"{monthNumber} - {monthName}"
                });
            }

            MonthComboBox.ItemsSource = items;
            MonthComboBox.DisplayMemberPath = "DisplayText";
            MonthComboBox.SelectedValuePath = "Value";

            _isUpdatingSelectors = false;
        }

        private void PopulateYearDropdown()
        {
            _isUpdatingSelectors = true;

            var items = new List<DropdownItem>();
            bool useNepaliNumbers = _localizationService.CurrentLanguage == AppLanguage.Nepali;

            foreach (var year in _converter.GetAvailableYears())
            {
                items.Add(new DropdownItem
                {
                    Value = year,
                    DisplayText = useNepaliNumbers
                        ? _nepaliNumberService.ToNepaliNumber(year)
                        : year.ToString()
                });
            }

            YearComboBox.ItemsSource = items;
            YearComboBox.DisplayMemberPath = "DisplayText";
            YearComboBox.SelectedValuePath = "Value";

            _isUpdatingSelectors = false;
        }

        private void SetSelectedDropdownValues()
        {
            _isUpdatingSelectors = true;

            MonthComboBox.SelectedValue = _currentMonth;
            YearComboBox.SelectedValue = _currentYear;
            LanguageComboBox.SelectedValue = _localizationService.CurrentLanguage;

            _isUpdatingSelectors = false;
        }

        private void PreviousMonth_Click(object sender, RoutedEventArgs e)
        {
            int newMonth = _currentMonth - 1;
            int newYear = _currentYear;

            if (newMonth < 1)
            {
                newMonth = 12;
                newYear--;
            }

            if (!_converter.GetAvailableYears().Contains(newYear))
                return;

            _currentMonth = newMonth;
            _currentYear = newYear;

            LoadCalendar();
        }

        private void TodayButton_Click(object sender, RoutedEventArgs e)
        {
            SelectToday();
        }

        private void SelectToday()
        {
            var todayBs = _converter.ConvertFromAd(DateTime.Today);

            _currentYear = todayBs.Year;
            _currentMonth = todayBs.Month;
            _selectedYear = todayBs.Year;
            _selectedMonth = todayBs.Month;
            _selectedDay = todayBs.Day;
            _hasSelectedDate = true;

            LoadDashboardData();
            LoadCalendar();
        }

        private void CalendarDayBorder_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Border border || border.DataContext is not CalendarCell cell)
                return;

            if (!cell.IsCurrentMonth || cell.Day <= 0)
                return;

            SelectDate(cell.Year, cell.Month, cell.Day);
        }

        private void CalendarDayBorder_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            // Only show the right-click menu on real day cells, not the leading/trailing blanks.
            if (sender is Border border &&
                border.DataContext is CalendarCell cell &&
                cell.IsCurrentMonth &&
                cell.Day > 0)
            {
                return;
            }

            e.Handled = true;
        }

        private void AddEventForCell_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem menuItem)
                return;

            var cell = menuItem.DataContext as CalendarCell
                ?? ((menuItem.Parent as ContextMenu)?.PlacementTarget as FrameworkElement)?.DataContext as CalendarCell;

            if (cell is null || !cell.IsCurrentMonth || cell.Day <= 0)
                return;

            SelectDate(cell.Year, cell.Month, cell.Day);
            OpenAddEvent();
        }

        private void SelectDate(int year, int month, int day)
        {
            _selectedYear = year;
            _selectedMonth = month;
            _selectedDay = day;
            _hasSelectedDate = true;

            _currentYear = year;
            _currentMonth = month;

            LoadDashboardData();
            LoadCalendar();
        }

        private void MoveSelectedDateByDays(int dayDelta)
        {
            if (!_hasSelectedDate)
                return;

            try
            {
                DateTime selectedAdDate = _converter.ConvertToAd(_selectedYear, _selectedMonth, _selectedDay);
                DateTime newAdDate = selectedAdDate.AddDays(dayDelta);
                var newBsDate = _converter.ConvertFromAd(newAdDate);

                if (!_converter.GetAvailableYears().Contains(newBsDate.Year))
                    return;

                SelectDate(newBsDate.Year, newBsDate.Month, newBsDate.Day);
            }
            catch
            {
                // Ignore navigation outside supported date range.
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.OriginalSource is ComboBox)
                return;

            switch (e.Key)
            {
                case Key.Left:
                    MoveSelectedDateByDays(-1);
                    e.Handled = true;
                    break;

                case Key.Right:
                    MoveSelectedDateByDays(1);
                    e.Handled = true;
                    break;

                case Key.Up:
                    MoveSelectedDateByDays(-7);
                    e.Handled = true;
                    break;

                case Key.Down:
                    MoveSelectedDateByDays(7);
                    e.Handled = true;
                    break;

                case Key.Enter:
                    LoadDashboardData();
                    RefreshDashboardBindings();
                    e.Handled = true;
                    break;
            }
        }

        private void GenerateJsonButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var generator = new BsCalendarJsonGenerator();
                generator.GenerateFromRawFile();

                MessageBox.Show(
                    "JSON file generated successfully.",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Generation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void NextMonth_Click(object sender, RoutedEventArgs e)
        {
            int newMonth = _currentMonth + 1;
            int newYear = _currentYear;

            if (newMonth > 12)
            {
                newMonth = 1;
                newYear++;
            }

            if (!_converter.GetAvailableYears().Contains(newYear))
                return;

            _currentMonth = newMonth;
            _currentYear = newYear;

            LoadCalendar();
        }

        private void MonthComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingSelectors || MonthComboBox.SelectedValue == null)
                return;

            _currentMonth = (int)MonthComboBox.SelectedValue;
            LoadCalendar();
        }

        private void YearComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingSelectors || YearComboBox.SelectedValue == null)
                return;

            _currentYear = (int)YearComboBox.SelectedValue;
            LoadCalendar();
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingSelectors || LanguageComboBox.SelectedValue == null)
                return;

            _localizationService.CurrentLanguage = (AppLanguage)LanguageComboBox.SelectedValue;
            SaveLanguageSetting(_localizationService.CurrentLanguage);

            PopulateLanguageDropdown();
            PopulateMonthDropdown();
            PopulateYearDropdown();
            ApplyLocalizedText();
            LoadDashboardData();
            LoadCalendar();

            App.RefreshOpenWidgets();
        }

        private void ViewAllEventsButton_Click(object sender, RoutedEventArgs e)
        {
            OpenEventList();
        }

        private void ViewAllHolidaysButton_Click(object sender, RoutedEventArgs e)
        {
            OpenHolidayList();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            OpenSettings();
        }

        private void QuickActionButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.DataContext is not QuickActionItem action)
                return;

            switch (action.ActionKey)
            {
                case "add_event":
                    OpenAddEvent();
                    break;

                case "view_events":
                    OpenEventList();
                    break;

                case "converter":
                    OpenConverter();
                    break;

                case "export_calendar":
                    ExportCalendar();
                    break;

                default:
                    MessageBox.Show("Unknown action.", "Quick Action");
                    break;
            }
        }

        private void OpenAddEvent()
        {
            try
            {
                var dialog = new AddEventWindow(
                    _hasSelectedDate ? _selectedYear : null,
                    _hasSelectedDate ? _selectedMonth : null,
                    _hasSelectedDate ? _selectedDay : null)
                {
                    Owner = this
                };

                if (dialog.ShowDialog() == true)
                {
                    LoadDashboardData();
                    LoadCalendar();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Add Event",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void OpenEventList()
        {
            try
            {
                var window = new EventListWindow { Owner = this };
                window.ShowDialog();

                // Events may have been added, edited, or deleted while the list was open.
                LoadDashboardData();
                LoadCalendar();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Events",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void OpenHolidayList()
        {
            MessageBox.Show(
                "Holiday list placeholder.",
                "Holidays",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void OpenConverter()
        {
            try
            {
                var window = new ConverterWindow { Owner = this };
                window.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Converter",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ExportCalendar()
        {
            try
            {
                var events = _eventStore.GetAll();

                if (events.Count == 0)
                {
                    MessageBox.Show(
                        "There are no events to export yet.",
                        "Export",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "Export events",
                    FileName = "nepali-calendar-events",
                    DefaultExt = ".ics",
                    Filter = "iCalendar (*.ics)|*.ics|CSV spreadsheet (*.csv)|*.csv"
                };

                if (dialog.ShowDialog(this) != true)
                    return;

                bool isCsv = System.IO.Path.GetExtension(dialog.FileName)
                    .Equals(".csv", StringComparison.OrdinalIgnoreCase);

                string content = isCsv
                    ? _exportService.ToCsv(events)
                    : _exportService.ToICalendar(events);

                System.IO.File.WriteAllText(dialog.FileName, content, System.Text.Encoding.UTF8);

                MessageBox.Show(
                    $"Exported {events.Count} event(s) to:\n{dialog.FileName}",
                    "Export complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Export failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void OpenSettings()
        {
            try
            {
                var window = new SettingsWindow { Owner = this };

                if (window.ShowDialog() == true)
                    ReloadLanguageFromSettings();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Settings",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ReloadLanguageFromSettings()
        {
            var settings = _settingsService.Load();

            if (_localizationService.CurrentLanguage == settings.Language)
                return;

            _localizationService.CurrentLanguage = settings.Language;

            PopulateLanguageDropdown();
            PopulateMonthDropdown();
            PopulateYearDropdown();
            ApplyLocalizedText();
            LoadDashboardData();
            LoadCalendar();

            App.RefreshOpenWidgets();
        }

        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            App.CheckForShutdown();
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            if (!string.IsNullOrWhiteSpace(propertyName))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}