using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace NepaliCalendar.App
{
    public class EmptyCellToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isCurrentMonth && !isCurrentMonth)
                return Brushes.LightGray;

            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}