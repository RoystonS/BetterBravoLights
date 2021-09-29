using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace BravoLights.UI
{
    [ValueConversion(typeof(bool), typeof(Brush))]
    public class RedBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? Brushes.Red : Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
