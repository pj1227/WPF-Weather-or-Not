using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WeatherDashboard.Data.Entities;
using WeatherDashboard.Models;
using WeatherDashboard.Services.Interfaces;

namespace WeatherDashboard.ViewModels
{
    public partial class DashboardViewModel : ViewModelBase
    {
        private readonly IWeatherService _weatherService;

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

        public string FormattedTemperature
        {
            get
            {
                if (CurrentWeather == null) return "--°";
                var temp = UseCelsius ? CurrentWeather.Temperature : (CurrentWeather.Temperature * 9 / 5) + 32;
                var unit = UseCelsius ? "C" : "F";
                return $"{temp:F1}°{unit}";
            }
        }

        public string FormattedFeelsLike
        {
            get
            {
                if (CurrentWeather == null) return "--°";
                var temp = UseCelsius ? CurrentWeather.FeelsLike : (CurrentWeather.FeelsLike * 9 / 5) + 32;
                return $"{temp:F1}°";
            }
        }

        public DashboardViewModel(IWeatherService weatherService, IDataService dataService, ILocationService locationService)
            : base(dataService, locationService)
        {
            _weatherService = weatherService;
        }

        public override async Task InitializeAsync()
        {
            // If selected location is null, set to default location from settings
            await base.InitializeAsync();
            // Load settings and locations first, then set SelectedLocation after IsBusy is false
            await ExecuteAsync(async () =>
            {
                // Load temperature unit preference
                var tempUnit = await DataService.GetSettingAsync("TemperatureUnit", "Celsius");
                UseCelsius = tempUnit == "Celsius";

                // Load API key from settings
                var apiKey = await DataService.GetSettingAsync("ApiKey");
                if (!string.IsNullOrEmpty(apiKey))
                {
                    _weatherService.SetApiKey(apiKey);
                }

                // Load all locations
                var allLocations = await DataService.GetAllLocationsAsync();
                Locations = new ObservableCollection<SavedLocation>(allLocations);
            }, "Failed to initialize dashboard");

            // Set SelectedLocation AFTER ExecuteAsync completes (IsBusy is now false)
            SelectedLocation = LocationService.SelectedLocation;
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

                await DataService.SaveWeatherRecordAsync(record);

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
                var existingLocation = await DataService.GetLocationByNameAsync(weather.LocationName);

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

                    SelectedLocation = await DataService.AddLocationAsync(newLocation);

                    // Reload locations
                    var allLocations = await DataService.GetAllLocationsAsync();
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
            // update global state with the new selection
            LocationService.SelectedLocation = value; 
            if (value != null)
            {
                _ = LoadWeatherAsync();
            }
        }

        [RelayCommand]
        private async Task SetDefaultLocationAsync()
        {
            if (SelectedLocation == null) return;

            await ExecuteAsync(async () =>
            {
                await DataService.SaveSettingAsync("DefaultLocationId", SelectedLocation.Id.ToString());

                // Show brief success message
                var originalError = ErrorMessage;
                ErrorMessage = $"? {SelectedLocation.Name} set as default location";

                await Task.Delay(2000);
                ErrorMessage = originalError;

            }, "Failed to set default location");
        }

        [RelayCommand]
        private async Task ToggleTemperatureUnitAsync()
        {
            UseCelsius = !UseCelsius;
            await DataService.SaveSettingAsync("TemperatureUnit", UseCelsius ? "Celsius" : "Fahrenheit");
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
        partial void OnUseCelsiusChanged(bool value)
        {
            OnPropertyChanged(nameof(FormattedTemperature));
            OnPropertyChanged(nameof(FormattedFeelsLike));
        }

        partial void OnCurrentWeatherChanged(WeatherData? value)
        {
            OnPropertyChanged(nameof(FormattedTemperature));
            OnPropertyChanged(nameof(FormattedFeelsLike));
        }
    }
}