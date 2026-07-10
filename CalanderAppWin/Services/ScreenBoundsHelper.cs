using System;
using System.Windows;
using System.Windows.Media;

namespace NepaliCalendar.App.Services
{
    /// <summary>
    /// Resolves the working area of the monitor a window actually sits on. The old logic clamped
    /// widgets against <c>SystemParameters.WorkArea</c> (the primary monitor only), which yanked a
    /// widget saved on a second monitor back onto the primary at every launch.
    /// </summary>
    public static class ScreenBoundsHelper
    {
        /// <summary>
        /// Working area, in device-independent WPF units, of the monitor containing
        /// <paramref name="window"/>. Falls back to the primary work area if it cannot be resolved.
        /// </summary>
        public static Rect GetWorkingAreaForWindow(Window window)
        {
            try
            {
                var dpi = VisualTreeHelper.GetDpi(window);
                double scaleX = dpi.DpiScaleX <= 0 ? 1.0 : dpi.DpiScaleX;
                double scaleY = dpi.DpiScaleY <= 0 ? 1.0 : dpi.DpiScaleY;

                // Find the monitor by the window's top-left in physical pixels.
                int pxX = (int)Math.Round(window.Left * scaleX);
                int pxY = (int)Math.Round(window.Top * scaleY);

                var screen = System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point(pxX, pxY));
                var wa = screen.WorkingArea; // physical pixels

                // Back to DIPs so callers stay in WPF coordinates.
                return new Rect(
                    wa.Left / scaleX,
                    wa.Top / scaleY,
                    wa.Width / scaleX,
                    wa.Height / scaleY);
            }
            catch
            {
                return new Rect(
                    SystemParameters.WorkArea.Left,
                    SystemParameters.WorkArea.Top,
                    SystemParameters.WorkArea.Width,
                    SystemParameters.WorkArea.Height);
            }
        }
    }
}
