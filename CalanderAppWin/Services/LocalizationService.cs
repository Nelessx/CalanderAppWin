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

        private bool Nepali => CurrentLanguage == AppLanguage.Nepali;

        // --- Dashboard chrome ---
        public string GetAppTitle() => Nepali ? "नेपाली पात्रो" : "Nepali Calendar";
        public string GetSelectedDateBsLabel() => Nepali ? "चयन गरिएको मिति (वि.सं.)" : "Selected Date (BS)";
        public string GetCorrespondingAdLabel() => Nepali ? "सम्बन्धित मिति (ई.सं.)" : "Corresponding Date (AD)";
        public string GetSelectedDateHeader() => Nepali ? "चयन गरिएको मिति" : "Selected Date";
        public string GetEventsOnSelectedDateLabel() => Nepali ? "यस मितिका कार्यक्रमहरू" : "Events on selected date";
        public string GetHolidaysOnSelectedDateLabel() => Nepali ? "यस मितिका बिदाहरू" : "Holidays on selected date";
        public string GetNoEventsText() => Nepali ? "यस मितिमा कुनै कार्यक्रम छैन।" : "No events for this date.";
        public string GetNoHolidaysText() => Nepali ? "यस मितिमा कुनै बिदा छैन।" : "No holidays for this date.";
        public string GetUpcomingEventsHeader() => Nepali ? "आगामी कार्यक्रमहरू" : "Upcoming Events";
        public string GetHolidaysHeader() => Nepali ? "बिदाहरू" : "Holidays";
        public string GetViewAllText() => Nepali ? "सबै हेर्नुहोस्" : "View All";
        public string GetQuickActionsHeader() => Nepali ? "द्रुत कार्यहरू" : "Quick Actions";
        public string GetTodayBadgeText() => Nepali ? "आज" : "Today";

        // --- Legend ---
        public string GetLegendTodayText() => Nepali ? "आज" : "Today";
        public string GetLegendEventText() => Nepali ? "कार्यक्रम" : "Event";
        public string GetLegendHolidayText() => Nepali ? "बिदा" : "Holiday";

        // --- Footer ---
        public string GetFooterLanguageText() => Nepali ? "भाषा: नेपाली" : "Language: English";
        public string GetDashboardReadyText() => Nepali ? "ड्यासबोर्ड तयार छ" : "Dashboard ready";
        public string GetSelectedIsTodayText() => Nepali ? "चयन गरिएको मिति आज हो" : "Selected date is today";
        public string GetNoSelectedDateText() => Nepali ? "कुनै मिति चयन गरिएको छैन" : "No date selected";

        // --- Quick action titles (keyed by ActionKey) ---
        public string GetQuickActionTitle(string actionKey)
        {
            return actionKey switch
            {
                "add_event" => Nepali ? "कार्यक्रम थप्नुहोस्" : "Add Event",
                "view_events" => Nepali ? "कार्यक्रम हेर्नुहोस्" : "View Events",
                "converter" => Nepali ? "रूपान्तरक" : "Converter",
                "export_calendar" => Nepali ? "निर्यात गर्नुहोस्" : "Export Calendar",
                _ => actionKey
            };
        }
    }
}