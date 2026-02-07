using System;
using System.Collections.Generic;

namespace WeatherDashboard.Data.Entities
{
    public class SavedLocation
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Country { get; set; } = string.Empty;
        public bool IsFavorite { get; set; }
        public DateTime CreatedDate { get; set; }

        // Navigation property
        public ICollection<WeatherRecord> WeatherRecords { get; set; } = new List<WeatherRecord>();
    }
}