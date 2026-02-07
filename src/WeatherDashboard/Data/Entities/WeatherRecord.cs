using System;

namespace WeatherDashboard.Data.Entities
{
    public class WeatherRecord
    {
        public int Id { get; set; }
        public int LocationId { get; set; }
        public DateTime Timestamp { get; set; }
        public double Temperature { get; set; }
        public double FeelsLike { get; set; }
        public double Humidity { get; set; }
        public double Pressure { get; set; }
        public double WindSpeed { get; set; }
        public string Description { get; set; } = string.Empty;
        public string IconCode { get; set; } = string.Empty;

        // Navigation property
        public SavedLocation? Location { get; set; }
    }
}