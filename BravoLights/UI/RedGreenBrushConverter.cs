using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace BravoLights.UI
{
    public class MultiBrushConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var red = (bool)values[0];
                var green = (bool)values[1];

                if (red)
                {
                    return green ? Brushes.Orange : Brushes.Red;
                }
                else
                {
                    return green ? Brushes.Green : Brushes.Black;
                }
            } catch
            {
                return Brushes.Black;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
