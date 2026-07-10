using System;
using System.IO;
using System.Text.Json;
using NepaliCalendar.App.Models;

namespace NepaliCalendar.App.Services
{
    public class SettingsService
    {
        private readonly string _settingsFolder;
        private readonly string _settingsFilePath;

        public SettingsService()
        {
            _settingsFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "NepaliCalendar");

            _settingsFilePath = Path.Combine(_settingsFolder, "appsettings.json");
        }

        public AppSettings Load()
        {
            // Live file first, then the .bak, then first-run defaults.
            if (TryLoadFrom(_settingsFilePath, out var settings))
                return settings;

            if (TryLoadFrom(_settingsFilePath + ".bak", out var backup))
            {
                Logger.Warn("appsettings.json was unreadable; recovered from backup copy.");
                return backup;
            }

            var defaults = new AppSettings();
            Save(defaults);
            return defaults;
        }

        private static bool TryLoadFrom(string path, out AppSettings settings)
        {
            settings = new AppSettings();

            try
            {
                if (!File.Exists(path))
                    return false;

                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json))
                    return false;

                var parsed = JsonSerializer.Deserialize<AppSettings>(json);
                if (parsed == null)
                    return false;

                settings = parsed;
                return true;
            }
            catch (Exception ex)
            {
                Logger.Warn($"Could not read settings at {path}.", ex);
                return false;
            }
        }

        public void Save(AppSettings settings)
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            // Crash-safe: temp file + atomic swap, keeping the previous copy as appsettings.json.bak.
            AtomicFile.WriteAllText(_settingsFilePath, json);
        }
    }
}