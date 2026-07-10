using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using NepaliCalendar.App.Models;
using NepaliCalendar.App.Services;

namespace NepaliCalendar.App.Views
{
    /// <summary>
    /// Interaction logic for ConverterWindow.xaml.
    /// A read-only utility that converts dates both ways using the existing
    /// <see cref="BsDateConverter"/> engine. Conversion is live as inputs change.
    /// </summary>
    public partial class ConverterWindow : Window
    {
        private readonly BsDateConverter _converter = new();
        private bool _isInitializing;
        private DateTime _minAd;
        private DateTime _maxAd;
        private BsDate? _lastAdToBsResult;

        /// <summary>Set when the user asks to jump the main calendar to a converted date.</summary>
        public (int Year, int Month, int Day)? NavigateTarget { get; private set; }

        public ConverterWindow()
        {
            InitializeComponent();

            try
            {
                InitializeInputs();
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

        private void InitializeInputs()
        {
            _isInitializing = true;

            var years = _converter.GetAvailableYears();
            var todayBs = _converter.ConvertFromAd(DateTime.Today);

            // --- BS -> AD inputs ---
            BsYearComboBox.ItemsSource = years;

            BsMonthComboBox.ItemsSource = Enumerable.Range(1, 12)
                .Select(m => new MonthOption(m, $"{m} - {_converter.GetNepaliMonthName(m)}"))
                .ToList();
            BsMonthComboBox.DisplayMemberPath = nameof(MonthOption.Text);
            BsMonthComboBox.SelectedValuePath = nameof(MonthOption.Value);

            BsYearComboBox.SelectedItem = years.Contains(todayBs.Year) ? todayBs.Year : years.FirstOrDefault();
            BsMonthComboBox.SelectedValue = todayBs.Month;

            _isInitializing = false;

            PopulateBsDayDropdown(todayBs.Day);

            // --- AD -> BS inputs, range-limited to the supported window ---
            _minAd = _converter.ConvertToAd(years.First(), 1, 1);
            int lastYear = years.Last();
            int lastMonthDays = _converter.GetMonthDays(lastYear, 12).Count;
            _maxAd = _converter.ConvertToAd(lastYear, 12, lastMonthDays);

            _isInitializing = true;

            AdYearComboBox.ItemsSource = Enumerable.Range(_minAd.Year, _maxAd.Year - _minAd.Year + 1).ToList();

            AdMonthComboBox.ItemsSource = Enumerable.Range(1, 12)
                .Select(m => new MonthOption(m, $"{m} - {new DateTime(2000, m, 1):MMMM}"))
                .ToList();
            AdMonthComboBox.DisplayMemberPath = nameof(MonthOption.Text);
            AdMonthComboBox.SelectedValuePath = nameof(MonthOption.Value);

            DateTime today = DateTime.Today;
            DateTime seed = today >= _minAd && today <= _maxAd ? today : _minAd;
            AdYearComboBox.SelectedItem = seed.Year;
            AdMonthComboBox.SelectedValue = seed.Month;

            _isInitializing = false;

            PopulateAdDayDropdown(seed.Day);

            ConvertBsToAd();
            ConvertAdToBs();
        }

        private void PopulateBsDayDropdown(int preferredDay)
        {
            if (BsYearComboBox.SelectedItem is not int year || BsMonthComboBox.SelectedValue is not int month)
                return;

            int daysInMonth;
            try
            {
                daysInMonth = _converter.GetMonthDays(year, month).Count;
            }
            catch
            {
                daysInMonth = 30;
            }

            var days = Enumerable.Range(1, daysInMonth).ToList();

            _isInitializing = true;
            BsDayComboBox.ItemsSource = days;
            // Keep the day if it still exists in the new month, otherwise clamp to its last day.
            BsDayComboBox.SelectedItem = days.Contains(preferredDay) ? preferredDay : days[^1];
            _isInitializing = false;
        }

        private void BsSelector_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing)
                return;

            int preferred = BsDayComboBox.SelectedItem is int existing ? existing : 1;
            PopulateBsDayDropdown(preferred);
            ConvertBsToAd();
        }

        private void BsDay_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing)
                return;

            ConvertBsToAd();
        }

        private void ConvertBsToAd()
        {
            if (BsYearComboBox.SelectedItem is not int year ||
                BsMonthComboBox.SelectedValue is not int month ||
                BsDayComboBox.SelectedItem is not int day)
            {
                BsToAdResultText.Text = "—";
                BsToAdDayText.Text = string.Empty;
                return;
            }

            try
            {
                DateTime adDate = _converter.ConvertToAd(year, month, day);
                BsToAdResultText.Text = adDate.ToString("MMMM d, yyyy");
                BsToAdDayText.Text = adDate.DayOfWeek.ToString();
            }
            catch (Exception ex)
            {
                BsToAdResultText.Text = "—";
                BsToAdDayText.Text = ex.Message;
            }
        }

        private void PopulateAdDayDropdown(int preferredDay)
        {
            if (AdYearComboBox.SelectedItem is not int year || AdMonthComboBox.SelectedValue is not int month)
                return;

            int daysInMonth = DateTime.DaysInMonth(year, month);
            var days = Enumerable.Range(1, daysInMonth).ToList();

            _isInitializing = true;
            AdDayComboBox.ItemsSource = days;
            AdDayComboBox.SelectedItem = days.Contains(preferredDay) ? preferredDay : days[^1];
            _isInitializing = false;
        }

        private void AdSelector_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing)
                return;

            int preferred = AdDayComboBox.SelectedItem is int existing ? existing : 1;
            PopulateAdDayDropdown(preferred);
            ConvertAdToBs();
        }

        private void AdDay_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing)
                return;

            ConvertAdToBs();
        }

        private void ConvertAdToBs()
        {
            // Can fire while the window is still loading; the named result elements exist after InitializeComponent.
            if (AdToBsResultText == null)
                return;

            _lastAdToBsResult = null;

            if (AdYearComboBox.SelectedItem is not int year ||
                AdMonthComboBox.SelectedValue is not int month ||
                AdDayComboBox.SelectedItem is not int day)
            {
                AdToBsResultText.Text = "—";
                AdToBsDayText.Text = string.Empty;
                return;
            }

            var adDate = new DateTime(year, month, day);

            if (adDate < _minAd || adDate > _maxAd)
            {
                AdToBsResultText.Text = "—";
                AdToBsDayText.Text = "Outside supported range";
                return;
            }

            try
            {
                var bs = _converter.ConvertFromAd(adDate);
                _lastAdToBsResult = bs;
                AdToBsResultText.Text = $"{_converter.GetNepaliMonthName(bs.Month)} {bs.Day}, {bs.Year}";
                AdToBsDayText.Text = bs.DayName;
            }
            catch
            {
                AdToBsResultText.Text = "—";
                AdToBsDayText.Text = "Outside supported range";
            }
        }

        private void OpenBsInCalendar_Click(object sender, RoutedEventArgs e)
        {
            if (BsYearComboBox.SelectedItem is int year &&
                BsMonthComboBox.SelectedValue is int month &&
                BsDayComboBox.SelectedItem is int day)
            {
                NavigateTarget = (year, month, day);
                DialogResult = true;
                Close();
            }
        }

        private void OpenAdResultInCalendar_Click(object sender, RoutedEventArgs e)
        {
            if (_lastAdToBsResult is { } bs)
            {
                NavigateTarget = (bs.Year, bs.Month, bs.Day);
                DialogResult = true;
                Close();
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private sealed record MonthOption(int Value, string Text);
    }
}
