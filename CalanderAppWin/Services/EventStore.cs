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

        // Parsed-file cache, validated against the file's last-write time so it stays correct even
        // when another EventStore instance (widget, dialog, reminder loop) writes the file.
        private List<CalendarEvent>? _cache;
        private DateTime _cacheStampUtc;

        private CalendarEvent? _lastDeleted;

        public EventStore(string? storageFolder = null)
        {
            _storeFolder = storageFolder ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "NepaliCalendar");

            _storeFilePath = Path.Combine(_storeFolder, "events.json");
        }

        public List<CalendarEvent> GetAll()
        {
            try
            {
                if (_cache != null && File.Exists(_storeFilePath) &&
                    File.GetLastWriteTimeUtc(_storeFilePath) == _cacheStampUtc)
                {
                    // Hand back a copy so callers (Add/Update) can mutate freely without
                    // corrupting the cache.
                    return new List<CalendarEvent>(_cache);
                }
            }
            catch
            {
                // Fall through to a fresh load.
            }

            // Try the live file first; if it is missing, unreadable, or corrupt, transparently
            // fall back to the .bak copy left behind by the last atomic write.
            if (TryLoadFrom(_storeFilePath, out var primary))
            {
                UpdateCache(primary);
                return new List<CalendarEvent>(primary);
            }

            if (TryLoadFrom(_storeFilePath + ".bak", out var backup))
            {
                Logger.Warn("events.json was unreadable; recovered from backup copy.");
                return new List<CalendarEvent>(backup);
            }

            return new List<CalendarEvent>();
        }

        private void UpdateCache(List<CalendarEvent> events)
        {
            _cache = new List<CalendarEvent>(events);
            try { _cacheStampUtc = File.GetLastWriteTimeUtc(_storeFilePath); }
            catch { _cacheStampUtc = default; }
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

            var removed = events.FirstOrDefault(e => e.Id == id);
            if (events.RemoveAll(e => e.Id == id) > 0)
            {
                _lastDeleted = removed;
                SaveAll(events);
            }
        }

        /// <summary>True when the last delete on this store can still be undone.</summary>
        public bool CanUndoDelete => _lastDeleted != null;

        /// <summary>Re-adds the most recently deleted event. Returns it, or null if nothing to undo.</summary>
        public CalendarEvent? RestoreLastDeleted()
        {
            if (_lastDeleted is null)
                return null;

            var restored = _lastDeleted;
            _lastDeleted = null;

            var events = GetAll();
            if (!events.Any(e => e.Id == restored.Id))
            {
                events.Add(restored);
                SaveAll(events);
            }

            return restored;
        }

        /// <summary>Bulk-adds imported events (each gets a fresh Id/timestamps). Returns the count added.</summary>
        public int AddRange(IEnumerable<CalendarEvent> incoming)
        {
            var events = GetAll();
            int added = 0;

            foreach (var e in incoming)
            {
                e.Id = Guid.NewGuid();
                e.CreatedUtc = DateTime.UtcNow;
                e.ModifiedUtc = e.CreatedUtc;
                events.Add(e);
                added++;
            }

            if (added > 0)
                SaveAll(events);

            return added;
        }

        private void SaveAll(List<CalendarEvent> events)
        {
            string json = JsonSerializer.Serialize(events, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            // Crash-safe: temp file + atomic swap, keeping the previous copy as events.json.bak.
            AtomicFile.WriteAllText(_storeFilePath, json);

            // Refresh the cache so the next read reflects this write without re-parsing.
            UpdateCache(events);
        }
    }
}
