using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Threading;
using NepaliCalendar.App.Models;

namespace NepaliCalendar.App.Services
{
    /// <summary>
    /// Watches user events and raises a notification when an event's reminder falls due. Runs on
    /// the UI dispatcher with a light 30-second poll; each reminder fires at most once per app run.
    /// A calendar that never reminds you is only half a calendar — this closes that gap.
    /// </summary>
    public sealed class ReminderService : IDisposable
    {
        private const int AllDayReminderHour = 9; // all-day reminders anchor to 9:00 AM

        private readonly EventStore _eventStore;
        private readonly Action<string, string> _notify;
        private readonly DispatcherTimer _timer;
        private readonly HashSet<Guid> _fired = new();

        public ReminderService(EventStore eventStore, Action<string, string> notify)
        {
            _eventStore = eventStore;
            _notify = notify;
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
            _timer.Tick += (_, _) => Check();
        }

        public void Start()
        {
            _timer.Start();
            Check();
        }

        private void Check()
        {
            DateTime now = DateTime.Now;

            foreach (var e in _eventStore.GetAll())
            {
                if (e.ReminderMinutesBefore is null || _fired.Contains(e.Id))
                    continue;

                DateTime? trigger = GetReminderTime(e);
                if (trigger is null)
                    continue;

                // Fire only when the trigger has just passed (within 2 minutes) — never for
                // long-past reminders (e.g. an event created on a past date).
                if (trigger.Value <= now && trigger.Value > now.AddMinutes(-2))
                {
                    _fired.Add(e.Id);
                    try { _notify(e.Title, BuildBody(e)); }
                    catch (Exception ex) { Logger.Warn("Reminder notification failed.", ex); }
                }
            }
        }

        /// <summary>When this event's reminder should fire, or null if it has none.</summary>
        public static DateTime? GetReminderTime(CalendarEvent e)
        {
            if (e.ReminderMinutesBefore is null)
                return null;

            DateTime baseTime = !e.IsAllDay && TryParseTime(e.TimeText, out var timeOfDay)
                ? e.AdDate.Date + timeOfDay
                : e.AdDate.Date.AddHours(AllDayReminderHour);

            return baseTime.AddMinutes(-e.ReminderMinutesBefore.Value);
        }

        private static string BuildBody(CalendarEvent e)
        {
            if (e.IsAllDay)
                return $"{e.AdDate:MMM d} · All day";

            return string.IsNullOrWhiteSpace(e.TimeText)
                ? $"{e.AdDate:MMM d}"
                : $"{e.AdDate:MMM d} · {e.TimeText}";
        }

        private static bool TryParseTime(string? text, out TimeSpan timeOfDay)
        {
            if (!string.IsNullOrWhiteSpace(text) &&
                DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            {
                timeOfDay = parsed.TimeOfDay;
                return true;
            }

            timeOfDay = default;
            return false;
        }

        public void Dispose() => _timer.Stop();
    }
}
