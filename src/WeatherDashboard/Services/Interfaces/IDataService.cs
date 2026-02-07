using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WeatherDashboard.Data.Entities;

namespace WeatherDashboard.Services.Interfaces
{
    public interface IDataService
    {
        // Weather Records
        Task SaveWeatherRecordAsync(WeatherRecord record);
        Task<List<WeatherRecord>> GetWeatherHistoryAsync(int locationId, DateTime from, DateTime to);

        // Locations
        Task<List<SavedLocation>> GetAllLocationsAsync();
        Task<SavedLocation?> GetLocationByIdAsync(int id);
        Task<SavedLocation?> GetLocationByNameAsync(string name);
        Task<SavedLocation> AddLocationAsync(SavedLocation location);
        Task DeleteLocationAsync(int id);
        Task<SavedLocation?> GetDefaultLocationAsync();

        // Settings
        Task<string?> GetSettingAsync(string key, string? defaultValue = null);
        Task SaveSettingAsync(string key, string value);
    }
}