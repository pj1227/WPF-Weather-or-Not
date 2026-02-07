using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using WeatherDashboard.Models;
using WeatherDashboard.Services.Interfaces;

namespace WeatherDashboard.Services
{
    public class WeatherApiService : IWeatherService
    {
        private readonly HttpClient _httpClient;
        private string _apiKey = "YOUR_API_KEY_HERE"; // Will be set from settings

        public WeatherApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("https://api.openweathermap.org/data/2.5/");
        }

        public void SetApiKey(string apiKey)
        {
            _apiKey = apiKey;
        }

        public async Task<WeatherData> GetCurrentWeatherAsync(string cityName)
        {
            var url = $"weather?q={Uri.EscapeDataString(cityName)}&appid={_apiKey}&units=metric";
            var json = await _httpClient.GetStringAsync(url);
            var response = JsonSerializer.Deserialize<CurrentWeatherResponse>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (response == null)
                throw new Exception("Failed to deserialize weather data");

            return MapToWeatherData(response);
        }

        public async Task<WeatherData> GetCurrentWeatherAsync(double lat, double lon)
        {
            var url = $"weather?lat={lat}&lon={lon}&appid={_apiKey}&units=metric";
            var json = await _httpClient.GetStringAsync(url);
            var response = JsonSerializer.Deserialize<CurrentWeatherResponse>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (response == null)
                throw new Exception("Failed to deserialize weather data");

            return MapToWeatherData(response);
        }

        public async Task<List<ForecastData>> GetForecastAsync(string cityName)
        {
            var url = $"forecast?q={Uri.EscapeDataString(cityName)}&appid={_apiKey}&units=metric";
            var json = await _httpClient.GetStringAsync(url);
            var response = JsonSerializer.Deserialize<ForecastResponse>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (response == null)
                throw new Exception("Failed to deserialize forecast data");

            return MapToForecastData(response);
        }

        public async Task<List<ForecastData>> GetForecastAsync(double lat, double lon)
        {
            var url = $"forecast?lat={lat}&lon={lon}&appid={_apiKey}&units=metric";
            var json = await _httpClient.GetStringAsync(url);
            var response = JsonSerializer.Deserialize<ForecastResponse>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (response == null)
                throw new Exception("Failed to deserialize forecast data");

            return MapToForecastData(response);
        }

        private WeatherData MapToWeatherData(CurrentWeatherResponse response)
        {
            return new WeatherData
            {
                LocationName = response.Name ?? "Unknown",
                Country = response.Sys?.Country ?? "Unknown",
                Latitude = response.Coord?.Lat ?? 0,
                Longitude = response.Coord?.Lon ?? 0,
                Temperature = response.Main?.Temp ?? 0,
                FeelsLike = response.Main?.Feels_Like ?? 0,
                Humidity = response.Main?.Humidity ?? 0,
                Pressure = response.Main?.Pressure ?? 0,
                WindSpeed = response.Wind?.Speed ?? 0,
                Description = response.Weather?.FirstOrDefault()?.Description ?? "Unknown",
                IconCode = response.Weather?.FirstOrDefault()?.Icon ?? "01d",
                Timestamp = DateTime.UtcNow
            };
        }

        private List<ForecastData> MapToForecastData(ForecastResponse response)
        {
            if (response.List == null)
                return new List<ForecastData>();

            return response.List
                .GroupBy(f => DateTimeOffset.FromUnixTimeSeconds(f.Dt).Date)
                .Select(g => g.OrderBy(f => Math.Abs(DateTimeOffset.FromUnixTimeSeconds(f.Dt).Hour - 12)).First())
                .Take(5)
                .Select(f => new ForecastData
                {
                    Date = DateTimeOffset.FromUnixTimeSeconds(f.Dt).DateTime,
                    TempMax = f.Main?.Temp_Max ?? 0,
                    TempMin = f.Main?.Temp_Min ?? 0,
                    Description = f.Weather?.FirstOrDefault()?.Description ?? "Unknown",
                    IconCode = f.Weather?.FirstOrDefault()?.Icon ?? "01d",
                    Humidity = f.Main?.Humidity ?? 0,
                    WindSpeed = f.Wind?.Speed ?? 0
                })
                .ToList();
        }

        #region API Response Models

        private class CurrentWeatherResponse
        {
            public CoordData? Coord { get; set; }
            public List<WeatherDescription>? Weather { get; set; }
            public MainData? Main { get; set; }
            public WindData? Wind { get; set; }
            public string? Name { get; set; }
            public SysData? Sys { get; set; }
        }

        private class ForecastResponse
        {
            public List<ForecastItem>? List { get; set; }
        }

        private class ForecastItem
        {
            public long Dt { get; set; }
            public MainData? Main { get; set; }
            public List<WeatherDescription>? Weather { get; set; }
            public WindData? Wind { get; set; }
        }

        private class CoordData
        {
            public double Lat { get; set; }
            public double Lon { get; set; }
        }

        private class MainData
        {
            public double Temp { get; set; }
            public double Feels_Like { get; set; }
            public double Humidity { get; set; }
            public double Pressure { get; set; }
            public double Temp_Min { get; set; }
            public double Temp_Max { get; set; }
        }

        private class WeatherDescription
        {
            public string? Main { get; set; }
            public string? Description { get; set; }
            public string? Icon { get; set; }
        }

        private class WindData
        {
            public double Speed { get; set; }
        }

        private class SysData
        {
            public string? Country { get; set; }
        }

        #endregion
    }
}