using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace NepaliCalendar.App.Services
{
    /// <summary>
    /// Switches the native Windows title bar between light and dark via the DWM
    /// immersive dark-mode attribute, so the OS chrome matches the app theme.
    /// </summary>
    public static class WindowChromeHelper
    {
        private const int DwmwaUseImmersiveDarkMode = 20;        // Windows 11 / Win10 20H1+
        private const int DwmwaUseImmersiveDarkModeOld = 19;     // older Win10 builds

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        public static void ApplyTitleBar(Window window, bool dark)
        {
            try
            {
                IntPtr hwnd = new WindowInteropHelper(window).Handle;
                if (hwnd == IntPtr.Zero)
                    return;

                int useDark = dark ? 1 : 0;

                if (DwmSetWindowAttribute(hwnd, DwmwaUseImmersiveDarkMode, ref useDark, sizeof(int)) != 0)
                    DwmSetWindowAttribute(hwnd, DwmwaUseImmersiveDarkModeOld, ref useDark, sizeof(int));
            }
            catch
            {
                // Title-bar theming is best-effort; never let it break the window.
            }
        }
    }
}
