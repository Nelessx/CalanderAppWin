using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
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

            // --- AD -> BS input, range-limited to the supported window ---
            DateTime minAd = _converter.ConvertToAd(years.First(), 1, 1);
            int lastYear = years.Last();
            int lastMonthDays = _converter.GetMonthDays(lastYear, 12).Count;
            DateTime maxAd = _converter.ConvertToAd(lastYear, 12, lastMonthDays);

            AdDatePicker.DisplayDateStart = minAd;
            AdDatePicker.DisplayDateEnd = maxAd;

            DateTime today = DateTime.Today;
            // Setting SelectedDate raises SelectedDateChanged, which runs ConvertAdToBs().
            AdDatePicker.SelectedDate = today >= minAd && today <= maxAd ? today : minAd;

            ConvertBsToAd();
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

        private void AdDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            ConvertAdToBs();
        }

        private void ConvertAdToBs()
        {
            // Can fire while the window is still loading; the named result elements exist after InitializeComponent.
            if (AdToBsResultText == null)
                return;

            if (AdDatePicker.SelectedDate is not DateTime adDate)
            {
                AdToBsResultText.Text = "—";
                AdToBsDayText.Text = string.Empty;
                return;
            }

            try
            {
                var bs = _converter.ConvertFromAd(adDate);
                AdToBsResultText.Text = $"{_converter.GetNepaliMonthName(bs.Month)} {bs.Day}, {bs.Year}";
                AdToBsDayText.Text = bs.DayName;
            }
            catch (Exception ex)
            {
                AdToBsResultText.Text = "—";
                AdToBsDayText.Text = ex.Message;
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private sealed record MonthOption(int Value, string Text);
    }
}
