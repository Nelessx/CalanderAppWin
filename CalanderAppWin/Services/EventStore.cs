using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using NepaliCalendar.App.Models;

namespace NepaliCalendar.App.Services
{
    /// <summary>
    /// File-backed CRUD store for user-created calendar events.
    /// Persists to %LocalAppData%\NepaliCalendar\events.json, mirroring SettingsService.
    /// Reads/writes on every call so multiple instances (e.g. the Add Event dialog and
    /// the main window) always see each other's changes.
    /// </summary>
    public class EventStore
    {
        private readonly string _storeFolder;
        private readonly string _storeFilePath;

        public EventStore(string? storageFolder = null)
        {
            _storeFolder = storageFolder ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "NepaliCalendar");

            _storeFilePath = Path.Combine(_storeFolder, "events.json");
        }

        public List<CalendarEvent> GetAll()
        {
            // Try the live file first; if it is missing, unreadable, or corrupt, transparently
            // fall back to the .bak copy left behind by the last atomic write.
            if (TryLoadFrom(_storeFilePath, out var primary))
                return primary;

            if (TryLoadFrom(_storeFilePath + ".bak", out var backup))
            {
                Logger.Warn("events.json was unreadable; recovered from backup copy.");
                return backup;
            }

            return new List<CalendarEvent>();
        }

        private static bool TryLoadFrom(string path, out List<CalendarEvent> events)
        {
            events = new List<CalendarEvent>();

            try
            {
                if (!File.Exists(path))
                    return false;

                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json))
                {
                    // An empty file is a legitimate "no events" state, not a corruption.
                    return true;
                }

                var parsed = JsonSerializer.Deserialize<List<CalendarEvent>>(json);
                if (parsed == null)
                    return false;

                events = parsed;
                return true;
            }
            catch (Exception ex)
            {
                Logger.Warn($"Could not read event store at {path}.", ex);
                return false;
            }
        }

        public List<CalendarEvent> GetForBsDate(int year, int month, int day)
        {
            return GetAll()
                .Where(e => e.BsYear == year && e.BsMonth == month && e.BsDay == day)
                .OrderBy(SortableTime)
                .ToList();
        }

        public List<CalendarEvent> GetUpcoming(DateTime fromAdDate, int max)
        {
            return GetAll()
                .Where(e => e.AdDate.Date >= fromAdDate.Date)
                .OrderBy(e => e.AdDate.Date)
                .ThenBy(SortableTime)
                .Take(max)
                .ToList();
        }

        /// <summary>
        /// Chronological sort key for events within a single day. All-day events (or
        /// events with no time) sort first; timed events sort by parsed time; anything
        /// unparseable sorts last so it never crashes the ordering.
        /// </summary>
        private static TimeSpan SortableTime(CalendarEvent calendarEvent)
        {
            if (calendarEvent.IsAllDay || string.IsNullOrWhiteSpace(calendarEvent.TimeText))
                return TimeSpan.MinValue;

            return DateTime.TryParse(calendarEvent.TimeText, out var parsed)
                ? parsed.TimeOfDay
                : TimeSpan.MaxValue;
        }

        public CalendarEvent Add(CalendarEvent calendarEvent)
        {
            var events = GetAll();

            if (calendarEvent.Id == Guid.Empty)
                calendarEvent.Id = Guid.NewGuid();

            calendarEvent.CreatedUtc = DateTime.UtcNow;
            calendarEvent.ModifiedUtc = calendarEvent.CreatedUtc;

            events.Add(calendarEvent);
            SaveAll(events);

            return calendarEvent;
        }

        /// <summary>Replaces the stored event with the same Id. Returns false if it no longer exists.</summary>
        public bool Update(CalendarEvent calendarEvent)
        {
            var events = GetAll();
            int index = events.FindIndex(e => e.Id == calendarEvent.Id);

            if (index < 0)
                return false;

            calendarEvent.CreatedUtc = events[index].CreatedUtc;
            calendarEvent.ModifiedUtc = DateTime.UtcNow;
            events[index] = calendarEvent;

            SaveAll(events);
            return true;
        }

        public void Delete(Guid id)
        {
            var events = GetAll();

            if (events.RemoveAll(e => e.Id == id) > 0)
                SaveAll(events);
        }

        private void SaveAll(List<CalendarEvent> events)
        {
            string json = JsonSerializer.Serialize(events, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            // Crash-safe: temp file + atomic swap, keeping the previous copy as events.json.bak.
            AtomicFile.WriteAllText(_storeFilePath, json);
        }
    }
}
