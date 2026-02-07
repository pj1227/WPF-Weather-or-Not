using System;

namespace WeatherDashboard.Models
{
    public class WeatherData
    {
        public string LocationName { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Temperature { get; set; }
        public double FeelsLike { get; set; }
        public double Humidity { get; set; }
        public double Pressure { get; set; }
        public double WindSpeed { get; set; }
        public string Description { get; set; } = string.Empty;
        public string IconCode { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}