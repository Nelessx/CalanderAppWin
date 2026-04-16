using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace NepaliCalendar.App.Converters
{
    public class SaturdayForegroundConverter : IValueConverter
    {
        private static readonly Brush SaturdayBrush = Brushes.Red;
        private static readonly Brush NormalBrush = Brushes.Black;
        private static readonly Brush EmptyBrush = Brushes.LightGray;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not int dayOfWeekIndex)
                return NormalBrush;

            if (parameter is string isCurrentMonthText &&
                bool.TryParse(isCurrentMonthText, out bool isCurrentMonth) &&
                !isCurrentMonth)
            {
                return EmptyBrush;
            }

            return dayOfWeekIndex == 6 ? SaturdayBrush : NormalBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}