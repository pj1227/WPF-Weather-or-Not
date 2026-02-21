using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeatherDashboard.Helpers
{
    public static class TemperatureFormatter
    {
        public static double ToFahrenheit(double celsius)
        {
            return (celsius * 9 / 5) + 32;
        }

        public static double ToCelsius(double fahrenheit)
        {
            return (fahrenheit - 32) * 5 / 9;
        }

        public static string Format(double celsius, bool useCelsius)
        {
            if (useCelsius)
                return $"{celsius:F1}°C";

            return $"{ToFahrenheit(celsius):F1}°F";
        }
    }
}

