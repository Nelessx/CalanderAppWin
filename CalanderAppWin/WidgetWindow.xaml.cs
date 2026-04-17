using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace NepaliCalendar.App
{
    public partial class WidgetWindow : WidgetBaseWindow
    {
        private readonly DispatcherTimer _midnightRefreshTimer;

        private int _displayYear;
        private int _displayMonth;

        private Point _mouseDownPoint;
        private bool _isDragging;

        public WidgetWindow()
        {
            InitializeComponent();

            var todayBs = Converter.ConvertFromAd(DateTime.Today);
            _displayYear = todayBs.Year;
            _displayMonth = todayBs.Month;

            LoadWidgetData();
            UpdateWidgetSizeMenuState();

            _midnightRefreshTimer = App.CreateMidnightRefreshTimer(RefreshAtMidnight);
            _midnightRefreshTimer.Start();

            Closed += WidgetWindow_Closed;
        }

        private void LoadWidgetData()
        {
            LoadLanguageFromSettings();

            var todayBs = Converter.ConvertFromAd(DateTime.Today);
            var todayAd = DateTime.Today;

            bool useNepaliNumbers = UseNepaliNumbers;

            string bsMonthName = LocalizationService.GetMonthName(todayBs.Month);
            string bsYearText = useNepaliNumbers
                ? NepaliNumberService.ToNepaliNumber(todayBs.Year)
                : todayBs.Year.ToString();

            string bsDayText = useNepaliNumbers
                ? NepaliNumberService.ToNepaliNumber(todayBs.Day)
                : todayBs.Day.ToString();

            string weekdayText = LocalizationService.CurrentLanguage == Models.AppLanguage.Nepali
                ? GetNepaliDayName(todayAd.DayOfWeek)
                : todayAd.DayOfWeek.ToString();

            string adDateText = LocalizationService.CurrentLanguage == Models.AppLanguage.Nepali
                ? $"{NepaliNumberService.ToNepaliNumber(todayAd.Day)} {todayAd:MMMM} {NepaliNumberService.ToNepaliNumber(todayAd.Year)}"
                : $"{todayAd:MMMM d, yyyy}";

            LargeBsMonthYearText.Text = $"{bsMonthName} {bsYearText}";
            LargeDayText.Text = bsDayText;
            LargeWeekdayText.Text = weekdayText;
            LargeAdDateText.Text = adDateText;

            LoadCalendarGrid();
        }

        public void RefreshWidget()
        {
            LoadWidgetData();
            UpdateWidgetSizeMenuState();
        }

        private void LoadCalendarGrid()
        {
            bool useNepaliNumbers = UseNepaliNumbers;

            CalendarMonthYearText.Text = $"{LocalizationService.GetMonthName(_displayMonth)} " +
                                         (useNepaliNumbers
                                             ? NepaliNumberService.ToNepaliNumber(_displayYear)
                                             : _displayYear.ToString());

            WeekHeaderItemsControl.ItemsSource = LocalizationService.GetWeekdayHeaders();

            var grid = Converter.GetMonthGrid(_displayYear, _displayMonth, useNepaliNumbers);
            WidgetCalendarGrid.ItemsSource = grid;
        }

        private void RefreshAtMidnight()
        {
            var todayBs = Converter.ConvertFromAd(DateTime.Today);

            _displayYear = todayBs.Year;
            _displayMonth = todayBs.Month;

            LoadWidgetData();
        }

        private void UpdateWidgetSizeMenuState()
        {
            SmallSizeMenuItem.IsChecked = false;
            MediumSizeMenuItem.IsChecked = false;
            LargeSizeMenuItem.IsChecked = true;
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

        private void PreviousMonthButton_Click(object sender, RoutedEventArgs e)
        {
            _displayMonth--;

            if (_displayMonth < 1)
            {
                _displayMonth = 12;
                _displayYear--;
            }

            LoadCalendarGrid();
        }

        private void NextMonthButton_Click(object sender, RoutedEventArgs e)
        {
            _displayMonth++;

            if (_displayMonth > 12)
            {
                _displayMonth = 1;
                _displayYear++;
            }

            LoadCalendarGrid();
        }

        private void RootBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
                return;

            _mouseDownPoint = e.GetPosition(this);
            _isDragging = false;
        }

        private void RootBorder_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
                return;

            Point currentPoint = e.GetPosition(this);

            if (!_isDragging &&
                (Math.Abs(currentPoint.X - _mouseDownPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                 Math.Abs(currentPoint.Y - _mouseDownPoint.Y) > SystemParameters.MinimumVerticalDragDistance))
            {
                _isDragging = true;
                DragMove();
                App.SaveWidgetPosition(this);
            }
        }

        private void RootBorder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
            }
        }

        private void OpenAppMenuItem_Click(object sender, RoutedEventArgs e)
        {
            App.OpenMainAppWindow();
        }

        private void SwitchToSmallMenuItem_Click(object sender, RoutedEventArgs e)
        {
            App.SwitchWidget(this, Models.WidgetSize.Small);
        }

        private void SwitchToMediumMenuItem_Click(object sender, RoutedEventArgs e)
        {
            App.SwitchWidget(this, Models.WidgetSize.Medium);
        }

        private void SwitchToLargeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            App.SwitchWidget(this, Models.WidgetSize.Large);
        }

        private void CloseMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void WidgetWindow_Closed(object sender, EventArgs e)
        {
            _midnightRefreshTimer.Stop();
            App.SaveWidgetPosition(this);
            App.CheckForShutdown();
        }
    }
}