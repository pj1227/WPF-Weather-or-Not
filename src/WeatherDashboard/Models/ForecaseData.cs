using System;

namespace WeatherDashboard.Models
{
    public class ForecastData
    {
        public DateTime Date { get; set; }
        public double TempMax { get; set; }
        public double TempMin { get; set; }
        public string Description { get; set; } = string.Empty;
        public string IconCode { get; set; } = string.Empty;
        public double Humidity { get; set; }
        public double WindSpeed { get; set; }
    }
}