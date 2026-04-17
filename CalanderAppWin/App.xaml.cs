using System;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using NepaliCalendar.App.Models;
using NepaliCalendar.App.Services;

namespace NepaliCalendar.App
{
    public partial class App : Application
    {
        private static readonly SettingsService _settingsService = new();

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var settings = _settingsService.Load();

            switch (settings.SelectedWidgetSize)
            {
                case WidgetSize.Small:
                    if (settings.HasSavedSmallWidgetPosition)
                    {
                        OpenWidget(
                            WidgetSize.Small,
                            settings.SmallWidgetLeft,
                            settings.SmallWidgetTop);
                    }
                    else
                    {
                        OpenWidget(WidgetSize.Small);
                    }
                    break;

                case WidgetSize.Medium:
                    if (settings.HasSavedMediumWidgetPosition)
                    {
                        OpenWidget(
                            WidgetSize.Medium,
                            settings.MediumWidgetLeft,
                            settings.MediumWidgetTop);
                    }
                    else
                    {
                        OpenWidget(WidgetSize.Medium);
                    }
                    break;

                default:
                    if (settings.HasSavedLargeWidgetPosition)
                    {
                        OpenWidget(
                            WidgetSize.Large,
                            settings.LargeWidgetLeft,
                            settings.LargeWidgetTop);
                    }
                    else
                    {
                        OpenWidget(WidgetSize.Large);
                    }
                    break;
            }
        }

        public static void OpenWidget(WidgetSize size, double? left = null, double? top = null)
        {
            Window widget = size switch
            {
                WidgetSize.Small => new WidgetSmallWindow(),
                WidgetSize.Medium => new WidgetMediumWindow(),
                _ => new WidgetWindow()
            };

            if (left.HasValue || top.HasValue)
            {
                widget.WindowStartupLocation = WindowStartupLocation.Manual;

                if (left.HasValue)
                    widget.Left = left.Value;

                if (top.HasValue)
                    widget.Top = top.Value;
            }

            widget.Show();

            if (left.HasValue || top.HasValue)
            {
                EnsureWidgetIsOnScreen(widget);
            }
        }

        public static void OpenMainAppWindow()
        {
            MainWindow mainWindow = null;

            foreach (Window window in Current.Windows)
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

        private static void EnsureWidgetIsOnScreen(Window widget)
        {
            double screenLeft = SystemParameters.WorkArea.Left;
            double screenTop = SystemParameters.WorkArea.Top;
            double screenRight = SystemParameters.WorkArea.Right;
            double screenBottom = SystemParameters.WorkArea.Bottom;

            widget.UpdateLayout();

            double maxLeft = Math.Max(screenLeft, screenRight - widget.ActualWidth);
            double maxTop = Math.Max(screenTop, screenBottom - widget.ActualHeight);

            if (widget.Left < screenLeft)
                widget.Left = screenLeft;
            else if (widget.Left > maxLeft)
                widget.Left = maxLeft;

            if (widget.Top < screenTop)
                widget.Top = screenTop;
            else if (widget.Top > maxTop)
                widget.Top = maxTop;
        }

        private static bool TryGetSavedWidgetPosition(WidgetSize size, out double left, out double top)
        {
            var settings = _settingsService.Load();

            switch (size)
            {
                case WidgetSize.Small:
                    if (settings.HasSavedSmallWidgetPosition)
                    {
                        left = settings.SmallWidgetLeft;
                        top = settings.SmallWidgetTop;
                        return true;
                    }
                    break;

                case WidgetSize.Medium:
                    if (settings.HasSavedMediumWidgetPosition)
                    {
                        left = settings.MediumWidgetLeft;
                        top = settings.MediumWidgetTop;
                        return true;
                    }
                    break;

                case WidgetSize.Large:
                    if (settings.HasSavedLargeWidgetPosition)
                    {
                        left = settings.LargeWidgetLeft;
                        top = settings.LargeWidgetTop;
                        return true;
                    }
                    break;
            }

            left = 0;
            top = 0;
            return false;
        }
        public static DispatcherTimer CreateMidnightRefreshTimer(Action refreshAction)
        {
            var timer = new DispatcherTimer();

            void ScheduleNextTick()
            {
                DateTime now = DateTime.Now;
                DateTime nextMidnight = now.Date.AddDays(1);
                TimeSpan interval = nextMidnight - now;

                if (interval <= TimeSpan.Zero)
                {
                    interval = TimeSpan.FromMinutes(1);
                }

                timer.Interval = interval;
            }

            timer.Tick += (_, _) =>
            {
                timer.Stop();

                refreshAction?.Invoke();

                ScheduleNextTick();
                timer.Start();
            };

            ScheduleNextTick();

            return timer;
        }
        public static void SwitchWidget(Window currentWindow, WidgetSize newSize)
        {
            SaveWidgetPosition(currentWindow);

            var settings = _settingsService.Load();
            settings.SelectedWidgetSize = newSize;
            _settingsService.Save(settings);

            if (TryGetSavedWidgetPosition(newSize, out double savedLeft, out double savedTop))
            {
                OpenWidget(newSize, savedLeft, savedTop);
            }
            else
            {
                OpenWidget(newSize, currentWindow.Left, currentWindow.Top);
            }

            currentWindow.Close();
        }

        public static void SaveWidgetPosition(Window window)
        {
            var settings = _settingsService.Load();

            if (window is WidgetSmallWindow)
            {
                settings.SmallWidgetLeft = window.Left;
                settings.SmallWidgetTop = window.Top;
                settings.HasSavedSmallWidgetPosition = true;
            }
            else if (window is WidgetMediumWindow)
            {
                settings.MediumWidgetLeft = window.Left;
                settings.MediumWidgetTop = window.Top;
                settings.HasSavedMediumWidgetPosition = true;
            }
            else if (window is WidgetWindow)
            {
                settings.LargeWidgetLeft = window.Left;
                settings.LargeWidgetTop = window.Top;
                settings.HasSavedLargeWidgetPosition = true;
            }

            _settingsService.Save(settings);
        }

        public static void CheckForShutdown()
        {
            bool hasOpenWindows = Current.Windows.Cast<Window>().Any(w => w.IsVisible);

            if (!hasOpenWindows)
            {
                Current.Shutdown();
            }
        }
    }
}