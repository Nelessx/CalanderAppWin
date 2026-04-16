using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace NepaliCalendar.App
{
    public class TodayBorderConverter : IValueConverter
    {
        private static readonly Brush TodayBorder = new SolidColorBrush(Color.FromRgb(30, 144, 255));
        private static readonly Brush NormalBorder = Brushes.Transparent;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isToday && isToday)
                return TodayBorder;

            return NormalBorder;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}