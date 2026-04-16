using NepaliCalendar.App.Models;

namespace NepaliCalendar.App.Services
{
    public class LocalizationService
    {
        public AppLanguage CurrentLanguage { get; set; } = AppLanguage.English;

        public string GetMonthName(int month)
        {
            return CurrentLanguage switch
            {
                AppLanguage.Nepali => month switch
                {
                    1 => "बैशाख",
                    2 => "जेठ",
                    3 => "असार",
                    4 => "श्रावण",
                    5 => "भदौ",
                    6 => "आश्विन",
                    7 => "कार्तिक",
                    8 => "मंसिर",
                    9 => "पुष",
                    10 => "माघ",
                    11 => "फाल्गुण",
                    12 => "चैत",
                    _ => "अज्ञात"
                },
                _ => month switch
                {
                    1 => "Baisakh",
                    2 => "Jestha",
                    3 => "Ashadh",
                    4 => "Shrawan",
                    5 => "Bhadra",
                    6 => "Ashwin",
                    7 => "Kartik",
                    8 => "Mangsir",
                    9 => "Poush",
                    10 => "Magh",
                    11 => "Falgun",
                    12 => "Chaitra",
                    _ => "Unknown"
                }
            };
        }

        public string[] GetWeekdayHeaders()
        {
            return CurrentLanguage switch
            {
                AppLanguage.Nepali => new[] { "आइत", "सोम", "मंगल", "बुध", "बिही", "शुक्र", "शनि" },
                _ => new[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" }
            };
        }

        public string GetPreviousText()
        {
            return CurrentLanguage == AppLanguage.Nepali ? "अघिल्लो" : "Previous";
        }

        public string GetNextText()
        {
            return CurrentLanguage == AppLanguage.Nepali ? "अर्को" : "Next";
        }

        public string GetTodayText()
        {
            return CurrentLanguage == AppLanguage.Nepali ? "आज" : "Today";
        }

        public string GetTotalDaysText()
        {
            return CurrentLanguage == AppLanguage.Nepali ? "जम्मा दिन" : "Total Days";
        }

        public string GetMonthLabelText()
        {
            return CurrentLanguage == AppLanguage.Nepali ? "महिना" : "Month";
        }

        public string GetYearLabelText()
        {
            return CurrentLanguage == AppLanguage.Nepali ? "वर्ष" : "Year";
        }

        public string GetLanguageLabelText()
        {
            return CurrentLanguage == AppLanguage.Nepali ? "भाषा" : "Language";
        }

        public string GetLanguageDisplayText(AppLanguage language)
        {
            return language switch
            {
                AppLanguage.Nepali => "नेपाली",
                _ => "English"
            };
        }
    }
}