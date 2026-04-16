using System;
using System.IO;

namespace NepaliCalendar.App.Services
{
    public class BsCalendarJsonGenerator
    {
        // Reuse the importer that knows how to parse raw text and write JSON
        private readonly BsCalendarImportService _importService = new();

        /// <summary>
        /// Reads the raw BS calendar file and creates/overwrites the JSON file.
        /// </summary>
        public void GenerateFromRawFile()
        {
            // AppContext.BaseDirectory points to the app's output folder (bin/.../net10.0-windows/)
            string baseDir = AppContext.BaseDirectory;

            // Full path to the raw input file
            string rawFilePath = Path.Combine(baseDir, "Data", "bs-calendar-raw.txt");

            // Full path to the JSON output file
            string jsonFilePath = Path.Combine(baseDir, "Data", "bs-calendar-data.json");

            // Make sure the raw file exists before trying to read it
            if (!File.Exists(rawFilePath))
                throw new FileNotFoundException($"Raw file not found: {rawFilePath}");

            // Read all raw text from the file
            string rawText = File.ReadAllText(rawFilePath);

            // Convert raw text into a list of BsYearData objects
            var years = _importService.ParseRawText(rawText);

            // Write those objects into the JSON file
            _importService.WriteJsonFile(jsonFilePath, years);
        }
    }
}