using NepaliCalendar.App.Models;
using NepaliCalendar.App.Services;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace NepaliCalendar.App
{
    public partial class WidgetWindow : Window
    {
        private readonly BsDateConverter _converter = new();
        private readonly LocalizationService _localizationService = new();
        private readonly NepaliNumberService _nepaliNumberService = new();
        private readonly SettingsService _settingsService = new();
        private readonly DispatcherTimer _midnightRefreshTimer;

        private int _displayYear;
        private int _displayMonth;

        private Point _mouseDownPoint;
        private bool _isDragging;

        public WidgetWindow()
        {
            InitializeComponent();

            var todayBs = _converter.ConvertFromAd(DateTime.Today);
            _displayYear = todayBs.Year;
            _displayMonth = todayBs.Month;

            LoadWidgetData();

            _midnightRefreshTimer = App.CreateMidnightRefreshTimer(RefreshAtMidnight);
            _midnightRefreshTimer.Start();

            Closed += WidgetWindow_Closed;
        }

        private void LoadWidgetData()
        {
            var settings = _settingsService.Load();
            _localizationService.CurrentLanguage = settings.Language;

            var todayBs = _converter.ConvertFromAd(DateTime.Today);
            var todayAd = DateTime.Today;

            bool useNepaliNumbers = _localizationService.CurrentLanguage == AppLanguage.Nepali;

            string bsMonthName = _localizationService.GetMonthName(todayBs.Month);
            string bsYearText = useNepaliNumbers
                ? _nepaliNumberService.ToNepaliNumber(todayBs.Year)
                : todayBs.Year.ToString();

            string bsDayText = useNepaliNumbers
                ? _nepaliNumberService.ToNepaliNumber(todayBs.Day)
                : todayBs.Day.ToString();

            string weekdayText = _localizationService.CurrentLanguage == AppLanguage.Nepali
                ? GetNepaliDayName(todayAd.DayOfWeek)
                : todayAd.DayOfWeek.ToString();

            string adDateText = _localizationService.CurrentLanguage == AppLanguage.Nepali
                ? $"{_nepaliNumberService.ToNepaliNumber(todayAd.Day)} {todayAd:MMMM} {_nepaliNumberService.ToNepaliNumber(todayAd.Year)}"
                : $"{todayAd:MMMM d, yyyy}";

            LargeBsMonthYearText.Text = $"{bsMonthName} {bsYearText}";
            LargeDayText.Text = bsDayText;
            LargeWeekdayText.Text = weekdayText;
            LargeAdDateText.Text = adDateText;

            LoadCalendarGrid();
        }

        private void LoadCalendarGrid()
        {
            bool useNepaliNumbers = _localizationService.CurrentLanguage == AppLanguage.Nepali;

            CalendarMonthYearText.Text = $"{_localizationService.GetMonthName(_displayMonth)} " +
                                         (useNepaliNumbers
                                             ? _nepaliNumberService.ToNepaliNumber(_displayYear)
                                             : _displayYear.ToString());

            WeekHeaderItemsControl.ItemsSource = _localizationService.GetWeekdayHeaders();

            var grid = _converter.GetMonthGrid(_displayYear, _displayMonth, useNepaliNumbers);
            WidgetCalendarGrid.ItemsSource = grid;
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

        private void OpenMainWindow()
        {
            MainWindow mainWindow = null;

            foreach (Window window in Application.Current.Windows)
            {
                if (window is MainWindow existingMainWindow)
                {
                    mainWindow = existingMainWindow;
                    break;
                }
            }

            if (mainWindow == null)
            {
                mainWindow = new MainWindow();
                mainWindow.Show();
            }
            else
            {
                if (!mainWindow.IsVisible)
                    mainWindow.Show();

                mainWindow.Activate();
            }
        }

        private void OpenAppMenuItem_Click(object sender, RoutedEventArgs e)
        {
            OpenMainWindow();
        }

        private void SwitchToSmallMenuItem_Click(object sender, RoutedEventArgs e)
        {
            App.SwitchWidget(this, WidgetSize.Small);
        }

        private void SwitchToMediumMenuItem_Click(object sender, RoutedEventArgs e)
        {
            App.SwitchWidget(this, WidgetSize.Medium);
        }

        private void SwitchToLargeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            App.SwitchWidget(this, WidgetSize.Large);
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
        private void RefreshAtMidnight()
        {
            var todayBs = _converter.ConvertFromAd(DateTime.Today);

            _displayYear = todayBs.Year;
            _displayMonth = todayBs.Month;

            LoadWidgetData();
        }
    }
}