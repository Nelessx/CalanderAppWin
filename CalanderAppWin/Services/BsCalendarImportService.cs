using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using NepaliCalendar.App.Models;

namespace NepaliCalendar.App.Services
{
    public class BsCalendarImportService
    {
        /// <summary>
        /// Converts raw text data into a list of BsYearData objects.
        ///
        /// Expected raw format:
        /// 2081:31,31,32,32,31,30,30,30,29,29,30,31
        /// 2082:30,32,31,32,31,30,30,30,29,30,29,31
        /// </summary>
        public List<BsYearData> ParseRawText(string rawText)
        {
            var result = new List<BsYearData>();

            // Prevent parsing an empty file
            if (string.IsNullOrWhiteSpace(rawText))
                throw new Exception("Raw BS calendar text is empty.");

            // Split the file into separate non-empty lines
            var lines = rawText
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToList();

            // Parse each line one by one
            foreach (var line in lines)
            {
                // Expected format: year:month1,month2,...,month12
                var parts = line.Split(':');

                if (parts.Length != 2)
                    throw new Exception($"Invalid line format: {line}");

                // Parse the year part
                if (!int.TryParse(parts[0].Trim(), out int year))
                    throw new Exception($"Invalid year value: {parts[0]}");

                // Parse the month day values
                var monthParts = parts[1]
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .ToList();

                // Each BS year must have exactly 12 months
                if (monthParts.Count != 12)
                    throw new Exception($"Year {year} must have exactly 12 month values.");

                var monthDays = new int[12];

                // Convert each month day value to int
                for (int i = 0; i < 12; i++)
                {
                    if (!int.TryParse(monthParts[i], out int days))
                        throw new Exception($"Invalid day count '{monthParts[i]}' in year {year}, month {i + 1}.");

                    monthDays[i] = days;
                }

                // Build the model object for this year
                result.Add(new BsYearData
                {
                    Year = year,
                    MonthDays = monthDays
                });
            }

            // Return the years sorted by year number
            return result
                .OrderBy(x => x.Year)
                .ToList();
        }

        /// <summary>
        /// Writes the parsed BS year data into a formatted JSON file.
        /// </summary>
        public void WriteJsonFile(string outputPath, List<BsYearData> years)
        {
            var json = JsonSerializer.Serialize(years, new JsonSerializerOptions
            {
                WriteIndented = true // makes JSON easier to read
            });

            File.WriteAllText(outputPath, json);
        }
    }
}