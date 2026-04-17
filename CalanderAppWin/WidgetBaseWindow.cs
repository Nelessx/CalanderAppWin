using NepaliCalendar.App.Models;
using NepaliCalendar.App.Services;
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
    }
}