using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace NepaliCalendar.App
{
    public partial class WidgetMediumWindow : WidgetBaseWindow
    {
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
            LoadLanguageFromSettings();

            var todayBs = Converter.ConvertFromAd(DateTime.Today);
            var todayAd = DateTime.Today;

            string weekdayText = LocalizationService.CurrentLanguage == Models.AppLanguage.Nepali
    ? GetNepaliDayName(todayAd.DayOfWeek)
    : todayAd.DayOfWeek.ToString();

            string bsMonthName = LocalizationService.GetMonthName(todayBs.Month);
            string bsYearText = FormatBsNumber(todayBs.Year);
            string bsDayText = FormatBsNumber(todayBs.Day);

            MediumWeekdayText.Text = weekdayText;

            if (LocalizationService.CurrentLanguage == Models.AppLanguage.Nepali)
            {
                MediumBsDateText.Text = $"{bsYearText} {bsMonthName} {bsDayText}";
            }
            else
            {
                MediumBsDateText.Text = $"{bsMonthName} {bsDayText}, {bsYearText}";
            }

            MediumAdDateText.Text = FormatAdDateMonthFirst(todayAd);
        }

        public void RefreshWidget()
        {
            LoadWidgetData();
            UpdateWidgetSizeMenuState();
        }

        private void UpdateWidgetSizeMenuState()
        {
            SmallSizeMenuItem.IsChecked = false;
            MediumSizeMenuItem.IsChecked = true;
            LargeSizeMenuItem.IsChecked = false;
        }

        private void OpenAppMenuItem_Click(object sender, RoutedEventArgs e)
        {
            App.OpenMainAppWindow();
        }

        private void RootBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (HasParent<Button>(e.OriginalSource))
                return;

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
                App.SaveWidgetPosition(this);
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

        private void WidgetMediumWindow_Closed(object sender, EventArgs e)
        {
            _midnightRefreshTimer.Stop();
            App.SaveWidgetPosition(this);
            App.CheckForShutdown();
        }
    }
}