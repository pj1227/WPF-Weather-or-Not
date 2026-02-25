using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using WeatherDashboard.Data.Entities;
using WeatherDashboard.Helpers;
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
        private ObservableCollection<SavedLocation> _locations = new();

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private DateTime _lastUpdated;

        public string FormattedTemperature
        {
            get
            {
                if (CurrentWeather == null) return "--°";
                return TemperatureFormatter.Format(CurrentWeather.Temperature, StateService.UseCelsius);
            }
        }

        public string FormattedFeelsLike
        {
            get
            {
                if (CurrentWeather == null) return "--°";
                return TemperatureFormatter.Format(CurrentWeather.FeelsLike, StateService.UseCelsius);
            }
        }

        public DashboardViewModel(IDataService dataService,
            IApplicationStateService stateService,
            IWeatherService weatherService)
            : base(dataService, stateService)
        {
            _weatherService = weatherService;

            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(IsBusy))
                {
                    SearchLocationCommand.NotifyCanExecuteChanged();
                }
            };
        }

        public override async Task InitializeAsync()
        {
            await ExecuteAsync(async () =>
            {
                var apiKey = await DataService.GetSettingAsync("ApiKey");
                if (!string.IsNullOrEmpty(apiKey))
                {
                    _weatherService.SetApiKey(apiKey);
                }

                var allLocations = await DataService.GetAllLocationsAsync();
                Locations = new ObservableCollection<SavedLocation>(allLocations);

            }, "Failed to initialize dashboard");

            if (SelectedLocation != null)
            {
                // Fixes issue where the SavedLocation instance in the list is not the
                // same object reference as StateService.SelectedLocation.
                SelectedLocation = Locations.FirstOrDefault(l => l.Id == SelectedLocation.Id);
                await LoadWeatherAsync();
            }
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
                CurrentWeather = await _weatherService.GetCurrentWeatherAsync(
                    SelectedLocation.Latitude,
                    SelectedLocation.Longitude);

                var forecastList = await _weatherService.GetForecastAsync(
                    SelectedLocation.Latitude,
                    SelectedLocation.Longitude);

                Forecast = new ObservableCollection<ForecastData>(
                    forecastList.Select(f => new ForecastData
                    {
                        Date = f.Date,
                        TempMax = f.TempMax,
                        TempMin = f.TempMin,
                        Description = f.Description,
                        IconCode = f.IconCode,
                        Humidity = f.Humidity,
                        WindSpeed = f.WindSpeed,
                        TempMaxDisplay = StateService.UseCelsius
                            ? f.TempMax
                            : TemperatureFormatter.ToFahrenheit(f.TempMax),
                        TempMinDisplay = StateService.UseCelsius
                            ? f.TempMin
                            : TemperatureFormatter.ToFahrenheit(f.TempMin)
                    }));

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
                var weather = await _weatherService.GetCurrentWeatherAsync(SearchText);

                var existingLocation = await DataService.GetLocationByNameAsync(weather.LocationName);

                if (existingLocation != null)
                {
                    SelectedLocation = existingLocation;
                }
                else
                {
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

                    var allLocations = await DataService.GetAllLocationsAsync();
                    Locations = new ObservableCollection<SavedLocation>(allLocations);
                }

                await LoadWeatherAsync();
                SearchText = string.Empty;

            }, "Location not found. Please check the spelling and try again");
        }

        private bool CanSearch() => !string.IsNullOrWhiteSpace(SearchText) && !IsBusy;

        [RelayCommand]
        private async Task RefreshAsync()
        {
            await LoadWeatherAsync();
        }

        [RelayCommand]
        private async Task SetDefaultLocationAsync()
        {
            if (SelectedLocation == null) return;

            await ExecuteAsync(async () =>
            {
                await DataService.SaveSettingAsync("DefaultLocationId", SelectedLocation.Id.ToString());

                var originalError = ErrorMessage;
                ErrorMessage = $"\u2713 {SelectedLocation.Name} set as default location";
                await Task.Delay(2000);
                ErrorMessage = originalError;

            }, "Failed to set default location");
        }

        protected override void OnTemperatureUnitChanged()
        {
            OnPropertyChanged(nameof(FormattedTemperature));
            OnPropertyChanged(nameof(FormattedFeelsLike));

            foreach (var item in Forecast)
            {
                item.TempMaxDisplay = StateService.UseCelsius
                    ? item.TempMax
                    : TemperatureFormatter.ToFahrenheit(item.TempMax);

                item.TempMinDisplay = StateService.UseCelsius
                    ? item.TempMin
                    : TemperatureFormatter.ToFahrenheit(item.TempMin);
            }
        }

        partial void OnCurrentWeatherChanged(WeatherData? value)
        {
            OnPropertyChanged(nameof(FormattedTemperature));
            OnPropertyChanged(nameof(FormattedFeelsLike));
        }

        partial void OnSearchTextChanged(string value)
        {
            SearchLocationCommand.NotifyCanExecuteChanged();
        }
    }
}