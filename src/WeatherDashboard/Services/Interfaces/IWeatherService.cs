using System.Collections.Generic;
using System.Threading.Tasks;
using WeatherDashboard.Models;

namespace WeatherDashboard.Services.Interfaces
{
    public interface IWeatherService
    {
        Task<WeatherData> GetCurrentWeatherAsync(string cityName);
        Task<WeatherData> GetCurrentWeatherAsync(double lat, double lon);
        Task<List<ForecastData>> GetForecastAsync(string cityName);
        Task<List<ForecastData>> GetForecastAsync(double lat, double lon);
        void SetApiKey(string apiKey);
    }
}