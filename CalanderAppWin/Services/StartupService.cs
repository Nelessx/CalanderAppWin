using System;
using Microsoft.Win32;

namespace NepaliCalendar.App.Services
{
    /// <summary>
    /// Controls whether the app launches automatically when the user signs in,
    /// via the per-user HKCU Run key (no admin rights required).
    /// </summary>
    public class StartupService
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "NepaliCalendar";

        public bool IsEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
                return key?.GetValue(ValueName) is string value && !string.IsNullOrWhiteSpace(value);
            }
            catch
            {
                return false;
            }
        }

        public void SetEnabled(bool enabled)
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
                ?? Registry.CurrentUser.CreateSubKey(RunKeyPath);

            if (key is null)
                return;

            if (enabled)
            {
                string? exePath = Environment.ProcessPath;

                if (!string.IsNullOrWhiteSpace(exePath))
                    key.SetValue(ValueName, $"\"{exePath}\"");
            }
            else if (key.GetValue(ValueName) is not null)
            {
                key.DeleteValue(ValueName, throwOnMissingValue: false);
            }
        }
    }
}
