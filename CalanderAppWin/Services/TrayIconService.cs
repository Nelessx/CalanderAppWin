using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace NepaliCalendar.App.Services
{
    /// <summary>
    /// A Windows notification-area (system tray) icon that keeps the app reachable
    /// even when every widget/window is closed. Uses WinForms NotifyIcon hosted on
    /// the WPF dispatcher thread.
    /// </summary>
    public class TrayIconService : IDisposable
    {
        private NotifyIcon? _notifyIcon;
        private Icon? _icon;

        public void Initialize()
        {
            _icon = CreateBrandedIcon();

            _notifyIcon = new NotifyIcon
            {
                Icon = _icon,
                Visible = true,
                Text = "Nepali Calendar"
            };

            var menu = new ContextMenuStrip();
            menu.Items.Add("Open Calendar", null, (_, _) => App.OpenMainAppWindow());
            menu.Items.Add("Show Widget", null, (_, _) => App.ShowWidgetFromTray());
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Quit", null, (_, _) => App.QuitApplication());

            _notifyIcon.ContextMenuStrip = menu;
            _notifyIcon.DoubleClick += (_, _) => App.OpenMainAppWindow();
        }

        private static Icon CreateBrandedIcon()
        {
            using var bitmap = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                using var background = new SolidBrush(Color.FromArgb(0x6D, 0x4A, 0xFF));
                using var path = RoundedRect(new Rectangle(2, 2, 28, 28), 7);
                g.FillPath(background, path);

                using var font = new Font("Segoe UI", 17, FontStyle.Bold, GraphicsUnit.Pixel);
                using var text = new SolidBrush(Color.White);
                using var format = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                g.DrawString("N", font, text, new RectangleF(2, 1, 28, 28), format);
            }

            // Icon.FromHandle does not own the handle; fine for a single app-lifetime icon.
            return Icon.FromHandle(bitmap.GetHicon());
        }

        private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        public void Dispose()
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _notifyIcon = null;
            }

            _icon?.Dispose();
            _icon = null;
        }
    }
}
