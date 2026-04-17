using NepaliCalendar.App.Models;
using NepaliCalendar.App.Services;
using System;
using System.Windows;

namespace NepaliCalendar.App
{
    public class WidgetBaseWindow : Window
    {
        protected readonly BsDateConverter Converter = new();
        protected readonly LocalizationService LocalizationService = new();
        protected readonly NepaliNumberService NepaliNumberService = new();
        protected readonly SettingsService SettingsService = new();

        protected void LoadLanguageFromSettings()
        {
            var settings = SettingsService.Load();
            LocalizationService.CurrentLanguage = settings.Language;
        }

        protected bool UseNepaliNumbers =>
            LocalizationService.CurrentLanguage == AppLanguage.Nepali;

        protected string GetNepaliDayName(DayOfWeek dayOfWeek)
        {
            return dayOfWeek switch
            {
                DayOfWeek.Sunday => "आइतबार",
                DayOfWeek.Monday => "सोमबार",
                DayOfWeek.Tuesday => "मंगलबार",
                DayOfWeek.Wednesday => "बुधबार",
                DayOfWeek.Thursday => "बिहिबार",
                DayOfWeek.Friday => "शुक्रबार",
                DayOfWeek.Saturday => "शनिबार",
                _ => ""
            };
        }

        protected string GetNepaliDayNameShort(DayOfWeek dayOfWeek)
        {
            return dayOfWeek switch
            {
                DayOfWeek.Sunday => "आइत",
                DayOfWeek.Monday => "सोम",
                DayOfWeek.Tuesday => "मंगल",
                DayOfWeek.Wednesday => "बुध",
                DayOfWeek.Thursday => "बिही",
                DayOfWeek.Friday => "शुक्र",
                DayOfWeek.Saturday => "शनि",
                _ => ""
            };
        }

        protected string FormatBsNumber(int value)
        {
            return UseNepaliNumbers
                ? NepaliNumberService.ToNepaliNumber(value)
                : value.ToString();
        }

        protected string FormatBsNumberTwoDigitsEnglishOnly(int value)
        {
            return UseNepaliNumbers
                ? NepaliNumberService.ToNepaliNumber(value)
                : value.ToString("D2");
        }

        protected string FormatAdDate(DateTime adDate)
        {
            return LocalizationService.CurrentLanguage == AppLanguage.Nepali
                ? $"{NepaliNumberService.ToNepaliNumber(adDate.Day)} {adDate:MMMM} {NepaliNumberService.ToNepaliNumber(adDate.Year)}"
                : $"{adDate:MMMM d, yyyy}";
        }

        protected string FormatAdDateMonthFirst(DateTime adDate)
        {
            return LocalizationService.CurrentLanguage == AppLanguage.Nepali
                ? $"{adDate:MMMM} {NepaliNumberService.ToNepaliNumber(adDate.Day)}, {NepaliNumberService.ToNepaliNumber(adDate.Year)}"
                : $"{adDate:MMMM d, yyyy}";
        }

        protected string FormatBsMonthYear(int month, int year)
        {
            return $"{LocalizationService.GetMonthName(month)} {FormatBsNumber(year)}";
        }
    }
}