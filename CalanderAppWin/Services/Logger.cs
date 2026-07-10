using System;
using System.IO;
using System.Text;

namespace NepaliCalendar.App.Services
{
    /// <summary>
    /// Minimal, dependency-free, thread-safe file logger. Every error surfaced to the user (and
    /// any handled crash) is also appended here so a field failure leaves a trace instead of
    /// vanishing with the MessageBox. Logs live under %LocalAppData%\NepaliCalendar\logs and are
    /// size-rotated so they never grow without bound.
    /// </summary>
    public static class Logger
    {
        private const long MaxLogBytes = 1_000_000; // ~1 MB before rotating to app-log.1.txt
        private static readonly object Gate = new();

        private static readonly string LogFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NepaliCalendar",
            "logs");

        private static readonly string LogFilePath = Path.Combine(LogFolder, "app-log.txt");

        public static void Info(string message) => Write("INFO", message, null);

        public static void Warn(string message, Exception? ex = null) => Write("WARN", message, ex);

        public static void Error(string message, Exception? ex = null) => Write("ERROR", message, ex);

        private static void Write(string level, string message, Exception? ex)
        {
            try
            {
                lock (Gate)
                {
                    if (!Directory.Exists(LogFolder))
                        Directory.CreateDirectory(LogFolder);

                    RotateIfNeeded();

                    var sb = new StringBuilder();
                    sb.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"))
                      .Append(" [").Append(level).Append("] ")
                      .Append(message);

                    if (ex != null)
                    {
                        sb.AppendLine();
                        sb.Append(ex.GetType().FullName).Append(": ").Append(ex.Message);
                        sb.AppendLine();
                        sb.Append(ex.StackTrace);
                    }

                    sb.AppendLine();

                    File.AppendAllText(LogFilePath, sb.ToString(), Encoding.UTF8);
                }
            }
            catch
            {
                // Logging must never throw into the caller — a broken log sink is not a crash.
            }
        }

        private static void RotateIfNeeded()
        {
            try
            {
                var info = new FileInfo(LogFilePath);
                if (!info.Exists || info.Length < MaxLogBytes)
                    return;

                string rolled = Path.Combine(LogFolder, "app-log.1.txt");
                if (File.Exists(rolled))
                    File.Delete(rolled);

                File.Move(LogFilePath, rolled);
            }
            catch
            {
                // If rotation fails, keep appending to the current file rather than crashing.
            }
        }
    }
}
