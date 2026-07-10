using Microsoft.Win32;
using NepaliCalendar.App.Models;

namespace NepaliCalendar.App.Services
{
    /// <summary>Reads the current Windows app theme (light/dark) from the registry.</summary>
    public static class SystemThemeHelper
    {
        private const string PersonalizeKey =
            @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

        /// <summary>The Windows app theme: <see cref="AppTheme.Dark"/> or <see cref="AppTheme.Light"/> (default).</summary>
        public static AppTheme GetWindowsTheme()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKey);
                // AppsUseLightTheme: 1 = light, 0 = dark. Absent → assume light.
                if (key?.GetValue("AppsUseLightTheme") is int value && value == 0)
                    return AppTheme.Dark;
            }
            catch
            {
                // Registry unavailable — fall back to light.
            }

            return AppTheme.Light;
        }
    }
}
