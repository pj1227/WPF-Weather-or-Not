using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.DirectoryServices.ActiveDirectory;
using System.Threading.Tasks;
using WeatherDashboard.Data.Entities;
using WeatherDashboard.Models;
using WeatherDashboard.Services.Interfaces;

namespace WeatherDashboard.ViewModels
{
    public partial class DashboardViewModel : ViewModelBase
    {
        private readonly IWeatherService _weatherService;
        private readonly IDataService _dataService;

        [ObservableProperty]
        private WeatherData? _currentWeather;

        [ObservableProperty]
        private ObservableCollection<ForecastData> _forecast = new();

        [ObservableProperty]
        private SavedLocation? _selectedLocation;

        [ObservableProperty]
        private ObservableCollection<SavedLocation> _locations = new();

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private DateTime _lastUpdated;

        [ObservableProperty]
        private bool _useCelsius = true;

        public DashboardViewModel(IWeatherService weatherService, IDataService dataService)
        {
            _weatherService = weatherService;
            _dataService = dataService;
        }

        public async Task InitializeAsync()
        {
            await ExecuteAsync(async () =>
            {
                // Load temperature unit preference
                var tempUnit = await _dataService.GetSettingAsync("TemperatureUnit", "Celsius");
                UseCelsius = tempUnit == "Celsius";

                // Load API key from settings
                var apiKey = await _dataService.GetSettingAsync("ApiKey");
                if (!string.IsNullOrEmpty(apiKey))
                {
                    _weatherService.SetApiKey(apiKey);
                }

                // Load all locations
                var allLocations = await _dataService.GetAllLocationsAsync();
                Locations = new ObservableCollection<SavedLocation>(allLocations);

                // Load default location
                SelectedLocation = await _dataService.GetDefaultLocationAsync();

                if (SelectedLocation != null)
                {
                    await LoadWeatherAsync();
                }
            }, "Failed to initialize dashboard");
        }

        [RelayCommand]
        private async Task LoadWeatherAsync()
        {
            if (SelectedLocation == null)
            {
                ErrorMessage = "Please select a location first";
                return;
            }

            await ExecuteAsync(async () =>
            {
                // Get current weather
                CurrentWeather = await _weatherService.GetCurrentWeatherAsync(
                    SelectedLocation.Latitude,
                    SelectedLocation.Longitude);

                // Get forecast
                var forecastList = await _weatherService.GetForecastAsync(
                    SelectedLocation.Latitude,
                    SelectedLocation.Longitude);

                Forecast = new ObservableCollection<ForecastData>(forecastList);

                // Save to database
                var record = new WeatherRecord
                {
                    LocationId = SelectedLocation.Id,
                    Timestamp = DateTime.Now,
                    Temperature = CurrentWeather.Temperature,
                    FeelsLike = CurrentWeather.FeelsLike,
                    Humidity = CurrentWeather.Humidity,
                    Pressure = CurrentWeather.Pressure,
                    WindSpeed = CurrentWeather.WindSpeed,
                    Description = CurrentWeather.Description,
                    IconCode = CurrentWeather.IconCode
                };

                await _dataService.SaveWeatherRecordAsync(record);

                LastUpdated = DateTime.Now;

            }, "Failed to load weather data");
        }

        [RelayCommand(CanExecute = nameof(CanSearch))]
        private async Task SearchLocationAsync()
        {
            await ExecuteAsync(async () =>
            {
                // Get weather for the search text
                var weather = await _weatherService.GetCurrentWeatherAsync(SearchText);

                // Check if location already exists
                var existingLocation = await _dataService.GetLocationByNameAsync(weather.LocationName);

                if (existingLocation != null)
                {
                    SelectedLocation = existingLocation;
                }
                else
                {
                    // Create new location
                    var newLocation = new SavedLocation
                    {
                        Name = weather.LocationName,
                        Latitude = weather.Latitude,
                        Longitude = weather.Longitude,
                        Country = weather.Country,
                        CreatedDate = DateTime.Now,
                        IsFavorite = false
                    };

                    SelectedLocation = await _dataService.AddLocationAsync(newLocation);

                    // Reload locations
                    var allLocations = await _dataService.GetAllLocationsAsync();
                    Locations = new ObservableCollection<SavedLocation>(allLocations);
                }

                // Load weather
                await LoadWeatherAsync();

                // Clear search
                SearchText = string.Empty;

            }, "Location not found. Please check the spelling and try again");
        }

        private bool CanSearch() => !string.IsNullOrWhiteSpace(SearchText) && !IsBusy;

        [RelayCommand]
        private async Task RefreshAsync()
        {
            await LoadWeatherAsync();
        }

        partial void OnSelectedLocationChanged(SavedLocation? value)
        {
            if (value != null && CurrentWeather?.LocationName != value.Name)
            {
                _ = LoadWeatherAsync();
            }
        }

        public string GetFormattedTemperature(double temp)
        {
            if (UseCelsius)
                return $"{temp:F1}°C";
            else
            {
                var fahrenheit = (temp * 9 / 5) + 32;
                return $"{fahrenheit:F1}°F";
            }
        }
    }
}