using System;
using System.Linq;
using System.Windows;
using NepaliCalendar.App.Models;

namespace NepaliCalendar.App.Services
{
    /// <summary>
    /// Swaps the active theme ResourceDictionary at runtime so every control bound
    /// with DynamicResource updates live.
    /// </summary>
    public class ThemeService
    {
        // A key that only the theme dictionaries define, used to locate the current one.
        private const string MarkerKey = "WindowBrush";

        private static AppTheme _preference = AppTheme.Light;
        private static bool _listeningForOsChanges;

        /// <summary>The effective theme in effect (Light or Dark — never System).</summary>
        public static AppTheme Current { get; private set; } = AppTheme.Light;

        /// <summary>Resolves an <see cref="AppTheme"/> preference (System → the current OS theme).</summary>
        private static AppTheme Resolve(AppTheme preference) =>
            preference == AppTheme.System ? SystemThemeHelper.GetWindowsTheme() : preference;

        /// <summary>
        /// Applies a theme preference. "System" resolves to the current Windows theme and starts
        /// tracking OS theme changes live; Light/Dark stop that tracking.
        /// </summary>
        public void Apply(AppTheme preference)
        {
            _preference = preference;

            if (preference == AppTheme.System)
                StartListeningForOsChanges();
            else
                StopListeningForOsChanges();

            ApplyResolved(Resolve(preference));
        }

        private void ApplyResolved(AppTheme theme)
        {
            var app = Application.Current;
            if (app is null)
                return;

            Current = theme;

            string source = theme == AppTheme.Dark
                ? "Themes/DarkTheme.xaml"
                : "Themes/LightTheme.xaml";

            var newDict = new ResourceDictionary
            {
                Source = new Uri(source, UriKind.Relative)
            };

            var existing = app.Resources.MergedDictionaries
                .FirstOrDefault(d => d.Contains(MarkerKey));

            if (existing is not null)
                app.Resources.MergedDictionaries.Remove(existing);

            app.Resources.MergedDictionaries.Insert(0, newDict);

            // Match the native title bar of every open window to the theme.
            foreach (Window window in app.Windows)
                WindowChromeHelper.ApplyTitleBar(window, theme == AppTheme.Dark);
        }

        private void StartListeningForOsChanges()
        {
            if (_listeningForOsChanges)
                return;

            Microsoft.Win32.SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
            _listeningForOsChanges = true;
        }

        private void StopListeningForOsChanges()
        {
            if (!_listeningForOsChanges)
                return;

            Microsoft.Win32.SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
            _listeningForOsChanges = false;
        }

        private void OnUserPreferenceChanged(object sender, Microsoft.Win32.UserPreferenceChangedEventArgs e)
        {
            if (_preference != AppTheme.System || e.Category != Microsoft.Win32.UserPreferenceCategory.General)
                return;

            var resolved = Resolve(AppTheme.System);
            if (resolved == Current)
                return;

            // SystemEvents may fire off the UI thread; marshal the resource swap.
            Application.Current?.Dispatcher.Invoke(() => ApplyResolved(resolved));
        }
    }
}
