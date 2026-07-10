using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using NepaliCalendar.App.Models;
using NepaliCalendar.App.Services;

namespace NepaliCalendar.App.Views
{
    /// <summary>
    /// Interaction logic for AddEventWindow.xaml.
    /// Collects a new event from the user and persists it via <see cref="EventStore"/>.
    /// DialogResult is true when an event was saved.
    /// </summary>
    public partial class AddEventWindow : Window
    {
        private readonly BsDateConverter _converter = new();
        private readonly EventStore _eventStore = new();
        private bool _isInitializing;

        public AddEventWindow(int? bsYear = null, int? bsMonth = null, int? bsDay = null)
        {
            InitializeComponent();
            InitializeForm(bsYear, bsMonth, bsDay);
        }

        private void InitializeForm(int? bsYear, int? bsMonth, int? bsDay)
        {
            _isInitializing = true;

            var years = _converter.GetAvailableYears();
            YearComboBox.ItemsSource = years;

            MonthComboBox.ItemsSource = Enumerable.Range(1, 12)
                .Select(m => new MonthOption(m, $"{m} - {_converter.GetNepaliMonthName(m)}"))
                .ToList();
            MonthComboBox.DisplayMemberPath = nameof(MonthOption.Text);
            MonthComboBox.SelectedValuePath = nameof(MonthOption.Value);

            int selectedYear = bsYear ?? years.FirstOrDefault();
            int selectedMonth = (bsMonth is >= 1 and <= 12) ? bsMonth.Value : 1;

            YearComboBox.SelectedItem = years.Contains(selectedYear)
                ? selectedYear
                : years.FirstOrDefault();
            MonthComboBox.SelectedValue = selectedMonth;

            _isInitializing = false;

            PopulateDayDropdown(bsDay);
        }

        private void PopulateDayDropdown(int? preferredDay = null)
        {
            if (YearComboBox.SelectedItem is not int year || MonthComboBox.SelectedValue is not int month)
                return;

            int currentSelection = preferredDay
                ?? (DayComboBox.SelectedItem is int existing ? existing : 1);

            int daysInMonth;
            try
            {
                daysInMonth = _converter.GetMonthDays(year, month).Count;
            }
            catch
            {
                daysInMonth = 32;
            }

            var days = Enumerable.Range(1, daysInMonth).ToList();

            _isInitializing = true;
            DayComboBox.ItemsSource = days;
            DayComboBox.SelectedItem = days.Contains(currentSelection) ? currentSelection : 1;
            _isInitializing = false;
        }

        private void DateSelector_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing)
                return;

            PopulateDayDropdown();
        }

        private void AllDayCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (TimeTextBox == null)
                return;

            bool allDay = AllDayCheckBox.IsChecked == true;
            TimeTextBox.IsEnabled = !allDay;

            if (allDay)
                TimeTextBox.Clear();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            string title = TitleTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(title))
            {
                MessageBox.Show(
                    "Please enter a title for the event.",
                    "Title required",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                TitleTextBox.Focus();
                return;
            }

            if (YearComboBox.SelectedItem is not int year ||
                MonthComboBox.SelectedValue is not int month ||
                DayComboBox.SelectedItem is not int day)
            {
                MessageBox.Show(
                    "Please select a valid date.",
                    "Date required",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            DateTime adDate;
            try
            {
                adDate = _converter.ConvertToAd(year, month, day);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Invalid date",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            bool allDay = AllDayCheckBox.IsChecked == true;
            string? timeText = allDay || string.IsNullOrWhiteSpace(TimeTextBox.Text)
                ? null
                : TimeTextBox.Text.Trim();

            var calendarEvent = new CalendarEvent
            {
                Title = title,
                NepaliTitle = string.IsNullOrWhiteSpace(NepaliTitleTextBox.Text) ? null : NepaliTitleTextBox.Text.Trim(),
                AdDate = adDate,
                BsYear = year,
                BsMonth = month,
                BsDay = day,
                EventType = string.IsNullOrWhiteSpace(TypeTextBox.Text) ? "Event" : TypeTextBox.Text.Trim(),
                BadgeText = string.IsNullOrWhiteSpace(BadgeTextBox.Text) ? null : BadgeTextBox.Text.Trim(),
                IsAllDay = allDay,
                TimeText = timeText,
                IsHoliday = false,
                IsPublicHoliday = false,
                DayText = adDate.DayOfWeek.ToString()
            };

            try
            {
                _eventStore.Add(calendarEvent);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Could not save the event: " + ex.Message,
                    "Save failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private sealed record MonthOption(int Value, string Text);
    }
}
