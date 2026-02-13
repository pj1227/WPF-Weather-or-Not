using System;
using System.Globalization;
using System.Windows.Data;

namespace WeatherDashboard.Converters
{
    public class TempUnitConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool useCelsius)
            {
                return useCelsius ? "°C" : "°F";
            }
            return "°C";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}