using System;

namespace WeatherDashboard.Data.Entities
{
    public class UserSetting
    {
        public int Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public DateTime LastModified { get; set; }
    }
}