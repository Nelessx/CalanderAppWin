using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using NepaliCalendar.App.Models;
using NepaliCalendar.App.Services;

namespace NepaliCalendar.App
{
    public partial class App : Application
    {
        private static readonly SettingsService _settingsService = new();
        private static readonly ThemeService _themeService = new();
        private static TrayIconService? _trayIcon;
        private static SingleInstanceService? _singleInstance;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Only one instance may run: autostart + a persistent tray icon means a second manual
            // launch would duplicate everything and race on the shared JSON files. A later launch
            // instead surfaces the already-running instance and exits.
            _singleInstance = new SingleInstanceService();
            if (!_singleInstance.TryAcquire())
            {
                SingleInstanceService.SignalExistingInstance();
                _singleInstance.Dispose();
                _singleInstance = null;
                Shutdown();
                return;
            }

            _singleInstance.ListenForActivation(() =>
                Dispatcher.Invoke(() =>
                {
                    try
                    {
                        ShowWidgetFromTray();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Failed to surface app on second-instance activation.", ex);
                    }
                }));

            RegisterGlobalExceptionHandlers();

            // Match each window's native title bar to the current theme as it loads.
            EventManager.RegisterClassHandler(
                typeof(Window),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler((sender, _) =>
                {
                    if (sender is Window window)
                        WindowChromeHelper.ApplyTitleBar(window, ThemeService.Current == Models.AppTheme.Dark);
                }));

            try
            {
                _trayIcon = new TrayIconService();
                _trayIcon.Initialize();
            }
            catch
            {
                _trayIcon = null;
            }

            try
            {
                var settings = _settingsService.Load();
                _themeService.Apply(settings.Theme);
                OpenStartupWidget(settings);
            }
            catch (Exception ex)
            {
                ShowFriendlyError("The calendar could not open its widget. Opening the main window instead.", ex);

                try
                {
                    OpenMainAppWindow();
                }
                catch (Exception innerEx)
                {
                    ShowFriendlyError("The calendar could not start.", innerEx);
                    Shutdown();
                }
            }
        }

        private static void OpenStartupWidget(AppSettings settings)
        {
            switch (settings.SelectedWidgetSize)
            {
                case WidgetSize.Small:
                    if (settings.HasSavedSmallWidgetPosition)
                        OpenWidget(WidgetSize.Small, settings.SmallWidgetLeft, settings.SmallWidgetTop);
                    else
                        OpenWidget(WidgetSize.Small);
                    break;

                case WidgetSize.Medium:
                    if (settings.HasSavedMediumWidgetPosition)
                        OpenWidget(WidgetSize.Medium, settings.MediumWidgetLeft, settings.MediumWidgetTop);
                    else
                        OpenWidget(WidgetSize.Medium);
                    break;

                default:
                    if (settings.HasSavedLargeWidgetPosition)
                        OpenWidget(WidgetSize.Large, settings.LargeWidgetLeft, settings.LargeWidgetTop);
                    else
                        OpenWidget(WidgetSize.Large);
                    break;
            }
        }

        private void RegisterGlobalExceptionHandlers()
        {
            // UI-thread exceptions: report and keep the app alive where possible.
            DispatcherUnhandledException += (_, args) =>
            {
                ShowFriendlyError("An unexpected error occurred.", args.Exception);
                args.Handled = true;
                CheckForShutdown();
            };

            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            {
                if (args.ExceptionObject is Exception ex)
                    ShowFriendlyError("An unexpected error occurred.", ex);
            };

            TaskScheduler.UnobservedTaskException += (_, args) =>
            {
                args.SetObserved();
                ShowFriendlyError("An unexpected background error occurred.", args.Exception);
            };
        }

        private static void ShowFriendlyError(string message, Exception ex)
        {
            Logger.Error(message, ex);

            try
            {
                MessageBox.Show(
                    message + "\n\n" + ex.Message,
                    "Nepali Calendar",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch
            {
                // Never let error reporting itself bring down the app.
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
            MainWindow? mainWindow = null;

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

        public static void RefreshOpenWidgets()
        {
            foreach (Window window in Current.Windows)
            {
                if (window is WidgetSmallWindow smallWidget)
                {
                    smallWidget.RefreshWidget();
                }
                else if (window is WidgetMediumWindow mediumWidget)
                {
                    mediumWidget.RefreshWidget();
                }
                else if (window is WidgetWindow largeWidget)
                {
                    largeWidget.RefreshWidget();
                }
            }
        }

        private static void EnsureWidgetIsOnScreen(Window widget)
        {
            widget.UpdateLayout();

            // Clamp against the monitor the widget is actually on, not just the primary — so a
            // widget parked on a second screen isn't dragged back to the primary at launch.
            Rect workArea = ScreenBoundsHelper.GetWorkingAreaForWindow(widget);
            double screenLeft = workArea.Left;
            double screenTop = workArea.Top;
            double screenRight = workArea.Right;
            double screenBottom = workArea.Bottom;

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
            // With a tray icon the app intentionally stays alive when all windows
            // close; the user quits explicitly via the tray menu.
            if (_trayIcon != null)
                return;

            bool hasOpenWindows = Current.Windows.Cast<Window>().Any(w => w.IsVisible);

            if (!hasOpenWindows)
            {
                Current.Shutdown();
            }
        }

        /// <summary>Re-opens (or brings forward) a widget from the tray menu.</summary>
        public static void ShowWidgetFromTray()
        {
            foreach (Window window in Current.Windows)
            {
                if (window is WidgetBaseWindow existing)
                {
                    if (!existing.IsVisible)
                        existing.Show();

                    existing.Activate();
                    return;
                }
            }

            OpenStartupWidget(_settingsService.Load());
        }

        public static void ApplyTheme(AppTheme theme) => _themeService.Apply(theme);

        public static void QuitApplication()
        {
            _trayIcon?.Dispose();
            _trayIcon = null;
            _singleInstance?.Dispose();
            _singleInstance = null;
            Current.Shutdown();
        }
    }
}