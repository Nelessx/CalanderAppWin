using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace NepaliCalendar.App
{
    public partial class WidgetSmallWindow : WidgetBaseWindow
    {
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
            LoadLanguageFromSettings();

            var todayBs = Converter.ConvertFromAd(DateTime.Today);
            var todayAd = DateTime.Today;

            bool useNepaliNumbers = UseNepaliNumbers;

            SmallWeekdayText.Text = LocalizationService.CurrentLanguage == Models.AppLanguage.Nepali
                ? GetNepaliDayNameShort(todayAd.DayOfWeek)
                : todayAd.DayOfWeek.ToString()[..3].ToUpper();

            SmallDayText.Text = useNepaliNumbers
                ? NepaliNumberService.ToNepaliNumber(todayBs.Day)
                : todayBs.Day.ToString("D2");

            SmallMonthText.Text = useNepaliNumbers
                ? NepaliNumberService.ToNepaliNumber(todayBs.Month)
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

        private void OpenAppMenuItem_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            App.OpenMainAppWindow();
        }

        private void SwitchToSmallMenuItem_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            App.SwitchWidget(this, Models.WidgetSize.Small);
        }

        private void SwitchToMediumMenuItem_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            App.SwitchWidget(this, Models.WidgetSize.Medium);
        }

        private void SwitchToLargeMenuItem_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            App.SwitchWidget(this, Models.WidgetSize.Large);
        }

        private void CloseMenuItem_Click(object sender, System.Windows.RoutedEventArgs e)
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