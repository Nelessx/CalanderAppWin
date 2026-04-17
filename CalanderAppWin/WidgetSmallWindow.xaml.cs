using NepaliCalendar.App.Models;
using NepaliCalendar.App.Services;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace NepaliCalendar.App
{
    public partial class WidgetSmallWindow : Window
    {
        private readonly BsDateConverter _converter = new();
        private readonly LocalizationService _localizationService = new();
        private readonly NepaliNumberService _nepaliNumberService = new();
        private readonly SettingsService _settingsService = new();
        private readonly DispatcherTimer _midnightRefreshTimer;

        private Point _mouseDownPoint;
        private bool _isDragging;

        public WidgetSmallWindow()
        {
            InitializeComponent();
            LoadWidgetData();
            UpdateWidgetSizeMenuState();

            _midnightRefreshTimer = App.CreateMidnightRefreshTimer(LoadWidgetData);
            _midnightRefreshTimer.Start();

            Closed += WidgetSmallWindow_Closed;
        }

        private void LoadWidgetData()
        {
            var settings = _settingsService.Load();
            _localizationService.CurrentLanguage = settings.Language;

            var todayBs = _converter.ConvertFromAd(DateTime.Today);
            var todayAd = DateTime.Today;

            bool useNepaliNumbers = _localizationService.CurrentLanguage == AppLanguage.Nepali;

            SmallWeekdayText.Text = _localizationService.CurrentLanguage == AppLanguage.Nepali
                ? GetNepaliDayNameShort(todayAd.DayOfWeek)
                : todayAd.DayOfWeek.ToString()[..3].ToUpper();

            SmallDayText.Text = useNepaliNumbers
                ? _nepaliNumberService.ToNepaliNumber(todayBs.Day)
                : todayBs.Day.ToString("D2");

            SmallMonthText.Text = useNepaliNumbers
                ? _nepaliNumberService.ToNepaliNumber(todayBs.Month)
                : todayBs.Month.ToString("D2");
        }

        private void UpdateWidgetSizeMenuState()
        {
            SmallSizeMenuItem.IsChecked = true;
            MediumSizeMenuItem.IsChecked = false;
            LargeSizeMenuItem.IsChecked = false;
        }

        private string GetNepaliDayNameShort(DayOfWeek dayOfWeek)
        {
            return dayOfWeek switch
            {
                DayOfWeek.Sunday => "आइत",
                DayOfWeek.Monday => "सोम",
                DayOfWeek.Tuesday => "मंगल",
                DayOfWeek.Wednesday => "बुध",
                DayOfWeek.Thursday => "बिही",
                DayOfWeek.Friday => "शुक्र",
                DayOfWeek.Saturday => "शनि",
                _ => ""
            };
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

            var currentPoint = e.GetPosition(this);

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

        private void WidgetSmallWindow_Closed(object sender, EventArgs e)
        {
            _midnightRefreshTimer.Stop();
            App.SaveWidgetPosition(this);
            App.CheckForShutdown();
        }
    }
}