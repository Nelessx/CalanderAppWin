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

        public EventStore()
        {
            _storeFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "NepaliCalendar");

            _storeFilePath = Path.Combine(_storeFolder, "events.json");
        }

        public List<CalendarEvent> GetAll()
        {
            try
            {
                if (!File.Exists(_storeFilePath))
                    return new List<CalendarEvent>();

                string json = File.ReadAllText(_storeFilePath);
                var events = JsonSerializer.Deserialize<List<CalendarEvent>>(json);

                return events ?? new List<CalendarEvent>();
            }
            catch
            {
                return new List<CalendarEvent>();
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

        public void Update(CalendarEvent calendarEvent)
        {
            var events = GetAll();
            int index = events.FindIndex(e => e.Id == calendarEvent.Id);

            if (index < 0)
                return;

            calendarEvent.CreatedUtc = events[index].CreatedUtc;
            calendarEvent.ModifiedUtc = DateTime.UtcNow;
            events[index] = calendarEvent;

            SaveAll(events);
        }

        public void Delete(Guid id)
        {
            var events = GetAll();

            if (events.RemoveAll(e => e.Id == id) > 0)
                SaveAll(events);
        }

        private void SaveAll(List<CalendarEvent> events)
        {
            if (!Directory.Exists(_storeFolder))
                Directory.CreateDirectory(_storeFolder);

            string json = JsonSerializer.Serialize(events, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(_storeFilePath, json);
        }
    }
}
