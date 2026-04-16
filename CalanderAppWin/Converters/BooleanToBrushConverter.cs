using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace NepaliCalendar.App
{
    public class BooleanToBrushConverter : IValueConverter
    {
        // Background color for "today"
        private static readonly Brush TodayBackground = new SolidColorBrush(Color.FromRgb(220, 240, 255));

        // Normal background
        private static readonly Brush NormalBackground = Brushes.White;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isToday && isToday)
                return TodayBackground;

            return NormalBackground;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}