using System.Collections.Generic;
using NepaliCalendar.App.Models;

namespace NepaliCalendar.App.Services
{
    /// <summary>
    /// Supplies the fixed set of dashboard quick-action tiles. Titles are localized at display
    /// time via <see cref="LocalizationService.GetQuickActionTitle"/>; only the stable action key
    /// and glyph live here.
    /// </summary>
    public class QuickActionsService
    {
        public List<QuickActionItem> GetQuickActions()
        {
            return new List<QuickActionItem>
            {
                new QuickActionItem { Title = "Add Event", IconGlyph = "+", ActionKey = "add_event" },
                new QuickActionItem { Title = "View Events", IconGlyph = "•", ActionKey = "view_events" },
                new QuickActionItem { Title = "Converter", IconGlyph = "⇄", ActionKey = "converter" },
                new QuickActionItem { Title = "Export Calendar", IconGlyph = "↓", ActionKey = "export_calendar" }
            };
        }
    }
}
