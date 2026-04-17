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
    public partial class WidgetMediumWindow : Window
    {
        private readonly BsDateConverter _converter = new();
        private readonly LocalizationService _localizationService = new();
        private readonly NepaliNumberService _nepaliNumberService = new();
        private readonly SettingsService _settingsService = new();
        private readonly DispatcherTimer _midnightRefreshTimer;

        public WidgetMediumWindow()
        {
            InitializeComponent();
            LoadWidgetData();
            UpdateWidgetSizeMenuState();

            _midnightRefreshTimer = App.CreateMidnightRefreshTimer(LoadWidgetData);
            _midnightRefreshTimer.Start();

            Closed += WidgetMediumWindow_Closed;
        }

        private void LoadWidgetData()
        {
            var settings = _settingsService.Load();
            _localizationService.CurrentLanguage = settings.Language;

            var todayBs = _converter.ConvertFromAd(DateTime.Today);
            var todayAd = DateTime.Today;

            bool useNepaliNumbers = _localizationService.CurrentLanguage == AppLanguage.Nepali;

            string weekdayText = _localizationService.CurrentLanguage == AppLanguage.Nepali
                ? GetNepaliDayName(todayAd.DayOfWeek)
                : todayAd.DayOfWeek.ToString();

            string bsYearText = useNepaliNumbers
                ? _nepaliNumberService.ToNepaliNumber(todayBs.Year)
                : todayBs.Year.ToString();

            string bsMonthName = _localizationService.GetMonthName(todayBs.Month);

            string bsDayText = useNepaliNumbers
                ? _nepaliNumberService.ToNepaliNumber(todayBs.Day)
                : todayBs.Day.ToString();

            string adDateText = _localizationService.CurrentLanguage == AppLanguage.Nepali
                ? $"{todayAd:MMMM} {_nepaliNumberService.ToNepaliNumber(todayAd.Day)}, {_nepaliNumberService.ToNepaliNumber(todayAd.Year)}"
                : $"{todayAd:MMMM d, yyyy}";

            MediumWeekdayText.Text = weekdayText;

            if (_localizationService.CurrentLanguage == AppLanguage.Nepali)
            {
                MediumBsDateText.Text = $"{bsYearText} {bsMonthName} {bsDayText}";
            }
            else
            {
                MediumBsDateText.Text = $"{bsMonthName} {bsDayText}, {bsYearText}";
            }

            MediumAdDateText.Text = adDateText;
        }

        private void UpdateWidgetSizeMenuState()
        {
            SmallSizeMenuItem.IsChecked = false;
            MediumSizeMenuItem.IsChecked = true;
            LargeSizeMenuItem.IsChecked = false;
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

        private void SaveCurrentPosition()
        {
            App.SaveWidgetPosition(this);
        }

        private void RootBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (HasParent<Button>(e.OriginalSource))
                return;

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
                SaveCurrentPosition();
            }
        }

        private static bool HasParent<T>(object source) where T : DependencyObject
        {
            if (source is not DependencyObject current)
                return false;

            while (current != null)
            {
                if (current is T)
                    return true;

                current = VisualTreeHelper.GetParent(current);
            }

            return false;
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

        private void WidgetMediumWindow_Closed(object sender, EventArgs e)
        {
            _midnightRefreshTimer.Stop();
            App.SaveWidgetPosition(this);
            App.CheckForShutdown();
        }

    }
}