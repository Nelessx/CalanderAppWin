using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using NepaliCalendar.App.Models;

namespace NepaliCalendar.App.Services
{
    public class BsCalendarDataService
    {
        private readonly string _filePath;
        private List<BsYearData>? _cachedYears;

        public BsCalendarDataService()
        {
            _filePath = Path.Combine(AppContext.BaseDirectory, "Data", "bs-calendar-data.json");
        }

        public List<BsYearData> GetYears()
        {
            if (_cachedYears != null)
            {
                return _cachedYears;
            }

            if (!File.Exists(_filePath))
            {
                throw new FileNotFoundException($"BS calendar data file not found: {_filePath}");
            }

            string json = File.ReadAllText(_filePath);

            var years = JsonSerializer.Deserialize<List<BsYearData>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (years == null || years.Count == 0)
            {
                throw new Exception("BS calendar data file is empty or invalid.");
            }

            ValidateYears(years);

            _cachedYears = years
                .OrderBy(y => y.Year)
                .ToList();

            return _cachedYears;
        }

        private static void ValidateYears(List<BsYearData> years)
        {
            var duplicateYears = years
                .GroupBy(y => y.Year)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateYears.Count > 0)
            {
                throw new Exception($"Duplicate BS year entries found: {string.Join(", ", duplicateYears)}");
            }

            foreach (var year in years)
            {
                if (year.MonthDays == null)
                {
                    throw new Exception($"Year {year.Year} has no monthDays value.");
                }

                if (year.MonthDays.Length != 12)
                {
                    throw new Exception($"Year {year.Year} must have exactly 12 months.");
                }

                for (int i = 0; i < year.MonthDays.Length; i++)
                {
                    int days = year.MonthDays[i];

                    if (days < 28 || days > 32)
                    {
                        throw new Exception(
                            $"Year {year.Year}, month {i + 1} has invalid day count: {days}. Expected 28 to 32.");
                    }
                }
            }

            var orderedYears = years
                .Select(y => y.Year)
                .OrderBy(y => y)
                .ToList();

            for (int i = 1; i < orderedYears.Count; i++)
            {
                if (orderedYears[i] != orderedYears[i - 1] + 1)
                {
                    throw new Exception(
                        $"BS years must be continuous. Gap found between {orderedYears[i - 1]} and {orderedYears[i]}.");
                }
            }
        }
    }
}