using NepaliCalendar.App.Models;
using NepaliCalendar.App.Services;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace NepaliCalendar.App
{
    public partial class MainWindow : Window
    {
        private readonly BsDateConverter _converter = new();
        private readonly NepaliNumberService _nepaliNumberService = new();
        private readonly LocalizationService _localizationService = new();
        private readonly SettingsService _settingsService = new();

        private int _currentYear;
        private int _currentMonth;
        private int _selectedYear;
        private int _selectedMonth;
        private int _selectedDay;
        private bool _hasSelectedDate;
        private bool _isUpdatingSelectors;

        public MainWindow()
        {
            InitializeComponent();
            Closed += MainWindow_Closed;


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

            string monthName = _localizationService.GetMonthName(_currentMonth);
            string yearText = useNepaliNumbers
                ? _nepaliNumberService.ToNepaliNumber(_currentYear)
                : _currentYear.ToString();

            string totalDaysText = useNepaliNumbers
                ? _nepaliNumberService.ToNepaliNumber(monthDays.Count)
                : monthDays.Count.ToString();

            BsDateText.Text = $"{monthName} {yearText}";
            UpdateSelectedDateInfo(monthDays.Count);
            CalendarGrid.ItemsSource = grid;

            SetSelectedDropdownValues();
            UpdateNavigationButtonStates();
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
            var todayBs = _converter.ConvertFromAd(DateTime.Today);
            _currentYear = todayBs.Year;
            _currentMonth = todayBs.Month;
            _selectedYear = todayBs.Year;
            _selectedMonth = todayBs.Month;
            _selectedDay = todayBs.Day;
            _hasSelectedDate = true;

            LoadCalendar();
        }

        private void CalendarDayBorder_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is not Border border || border.DataContext is not CalendarCell cell)
                return;

            if (!cell.IsCurrentMonth || cell.Day <= 0)
                return;

            _selectedYear = cell.Year;
            _selectedMonth = cell.Month;
            _selectedDay = cell.Day;
            _hasSelectedDate = true;

            LoadCalendar();
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
            LoadCalendar();

            App.RefreshOpenWidgets();
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            App.CheckForShutdown();
        }
    }


}