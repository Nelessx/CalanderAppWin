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

        public void Apply(AppTheme theme)
        {
            var app = Application.Current;
            if (app is null)
                return;

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
        }
    }
}
