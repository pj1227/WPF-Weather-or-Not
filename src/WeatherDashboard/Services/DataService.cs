using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WeatherDashboard.Data;
using WeatherDashboard.Data.Entities;
using WeatherDashboard.Services.Interfaces;

namespace WeatherDashboard.Services
{
    public class DataService : IDataService
    {
        private readonly WeatherDbContext _context;

        public DataService(WeatherDbContext context)
        {
            _context = context;
        }

        #region Weather Records

        public async Task SaveWeatherRecordAsync(WeatherRecord record)
        {
            _context.WeatherRecords.Add(record);
            await _context.SaveChangesAsync();
        }

        public async Task<List<WeatherRecord>> GetWeatherHistoryAsync(int locationId, DateTime from, DateTime to)
        {
            return await _context.WeatherRecords
                .Where(w => w.LocationId == locationId && w.Timestamp >= from && w.Timestamp <= to)
                .OrderBy(w => w.Timestamp)
                .ToListAsync();
        }

        #endregion

        #region Locations

        public async Task<List<SavedLocation>> GetAllLocationsAsync()
        {
            return await _context.SavedLocations
                .OrderByDescending(l => l.IsFavorite)
                .ThenBy(l => l.Name)
                .ToListAsync();
        }

        public async Task<SavedLocation?> GetLocationByIdAsync(int id)
        {
            return await _context.SavedLocations.FindAsync(id);
        }

        public async Task<SavedLocation?> GetLocationByNameAsync(string name)
        {
            return await _context.SavedLocations
                .FirstOrDefaultAsync(l => l.Name.ToLower() == name.ToLower());
        }

        public async Task<SavedLocation> AddLocationAsync(SavedLocation location)
        {
            var existing = await GetLocationByNameAsync(location.Name);
            if (existing != null)
                return existing;

            location.CreatedDate = DateTime.Now;
            _context.SavedLocations.Add(location);
            await _context.SaveChangesAsync();

            return location;
        }

        public async Task DeleteLocationAsync(int id)
        {
            var location = await _context.SavedLocations.FindAsync(id);
            if (location != null)
            {
                _context.SavedLocations.Remove(location);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<SavedLocation?> GetDefaultLocationAsync()
        {
            var defaultLocationIdStr = await GetSettingAsync("DefaultLocationId");

            if (int.TryParse(defaultLocationIdStr, out int defaultLocationId))
            {
                return await GetLocationByIdAsync(defaultLocationId);
            }

            return await _context.SavedLocations
                .OrderByDescending(l => l.IsFavorite)
                .ThenBy(l => l.CreatedDate)
                .FirstOrDefaultAsync();
        }

        #endregion

        #region Settings

        public async Task<string?> GetSettingAsync(string key, string? defaultValue = null)
        {
            var setting = await _context.UserSettings
                .FirstOrDefaultAsync(s => s.Key == key);

            return setting?.Value ?? defaultValue;
        }

        public async Task SaveSettingAsync(string key, string value)
        {
            var setting = await _context.UserSettings
                .FirstOrDefaultAsync(s => s.Key == key);

            if (setting != null)
            {
                setting.Value = value;
                setting.LastModified = DateTime.Now;
                _context.UserSettings.Update(setting);
            }
            else
            {
                setting = new UserSetting
                {
                    Key = key,
                    Value = value,
                    LastModified = DateTime.Now
                };
                _context.UserSettings.Add(setting);
            }

            await _context.SaveChangesAsync();
        }

        #endregion
    }
}