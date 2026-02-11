using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScottPlot;
using System.Collections.ObjectModel;
using System.IO;
using WeatherDashboard.Data.Entities;
using WeatherDashboard.Services.Interfaces;

namespace WeatherDashboard.ViewModels
{
    public partial class HistoryViewModel : ViewModelBase
    {
        private readonly IReportService _reportService;
        private bool _isInitialized = false;

        [ObservableProperty]
        private ObservableCollection<SavedLocation> _locations = new();

        [ObservableProperty]
        private SavedLocation? _selectedLocation;

        [ObservableProperty]
        private DateTime _startDate = DateTime.Now.AddDays(-30);

        [ObservableProperty]
        private DateTime _endDate = DateTime.Now;

        [ObservableProperty]
        private List<WeatherRecord> _weatherHistory = new();

        [ObservableProperty]
        private Plot _temperaturePlot = new Plot();

        [ObservableProperty]
        private Plot _humidityPlot = new Plot();

        [ObservableProperty]
        private bool _useCelsius = true;

        [ObservableProperty]
        private string _successMessage = string.Empty;

        public bool HasSuccess => !string.IsNullOrEmpty(SuccessMessage);

        public HistoryViewModel(IDataService dataService, IReportService reportService, ILocationService locationService) 
            : base(dataService, locationService)
        {
            _reportService = reportService;
        }

        public override async Task InitializeAsync()
        {
            // set selected location from global state BEFORE loading history, but AFTER base initialization
            await base.InitializeAsync();
            // Load settings and locations first, but don't set SelectedLocation until after ExecuteAsync completes
            await ExecuteAsync(async () =>
            {
                var tempUnit = await DataService.GetSettingAsync("TemperatureUnit", "Celsius");
                UseCelsius = tempUnit == "Celsius";

                var allLocations = await DataService.GetAllLocationsAsync();
                Locations = new ObservableCollection<SavedLocation>(allLocations);

                // Set flag but DON'T set SelectedLocation yet

            }, "Failed to initialize history view");

            // Set flag AFTER ExecuteAsync completes
            _isInitialized = true;

            // Now set SelectedLocation (IsBusy is false, _isInitialized is true)
            SelectedLocation = LocationService.SelectedLocation;
        }

        [RelayCommand]
        private async Task LoadHistoryAsync()
        {
            if (SelectedLocation == null)
            {
                ErrorMessage = "Please select a location first";
                return;
            }

            await ExecuteAsync(async () =>
            {
                WeatherHistory = await DataService.GetWeatherHistoryAsync(
                    SelectedLocation.Id,
                    StartDate,
                    EndDate);

                if (!WeatherHistory.Any())
                {
                    ErrorMessage = $"No weather data found for {SelectedLocation.Name} in the selected date range.";
                    ClearCharts();
                    return;
                }

                UpdateTemperatureChart();
                UpdateHumidityChart();

                SuccessMessage = $"Loaded {WeatherHistory.Count} weather records";
                OnPropertyChanged(nameof(HasSuccess));

                await Task.Delay(3000);
                SuccessMessage = string.Empty;
                OnPropertyChanged(nameof(HasSuccess));

            }, "Failed to load weather history");
        }

        private void UpdateTemperatureChart()
        {
            var plot = new Plot();

            if (!WeatherHistory.Any())
            {
                TemperaturePlot = plot;
                return;
            }

            var dates = WeatherHistory.Select(r => r.Timestamp.ToOADate()).ToArray();
            var temps = WeatherHistory.Select(r =>
                UseCelsius ? r.Temperature : CelsiusToFahrenheit(r.Temperature)
            ).ToArray();
            var feelsLike = WeatherHistory.Select(r =>
                UseCelsius ? r.FeelsLike : CelsiusToFahrenheit(r.FeelsLike)
            ).ToArray();

            var tempScatter = plot.Add.Scatter(dates, temps);
            tempScatter.Label = "Temperature";
            tempScatter.Color = Colors.Red;
            tempScatter.LineWidth = 2;
            tempScatter.MarkerSize = 5;

            var feelsScatter = plot.Add.Scatter(dates, feelsLike);
            feelsScatter.Label = "Feels Like";
            feelsScatter.Color = Colors.Orange;
            feelsScatter.LineWidth = 2;
            feelsScatter.MarkerSize = 5;
            feelsScatter.LinePattern = LinePattern.Dashed;

            plot.Axes.DateTimeTicksBottom();
            plot.XLabel("Date");
            plot.YLabel($"Temperature (°{(UseCelsius ? "C" : "F")})");
            plot.Title($"Temperature History - {SelectedLocation?.Name}");
            plot.ShowLegend(Alignment.UpperLeft);
            plot.Axes.Color(Colors.Gray);
            plot.Grid.MajorLineColor = Colors.LightGray;

            TemperaturePlot = plot;
        }

        private void UpdateHumidityChart()
        {
            var plot = new Plot();

            if (!WeatherHistory.Any())
            {
                HumidityPlot = plot;
                return;
            }

            var dates = WeatherHistory.Select(r => r.Timestamp.ToOADate()).ToArray();
            var humidity = WeatherHistory.Select(r => r.Humidity).ToArray();

            var humidityScatter = plot.Add.Scatter(dates, humidity);
            humidityScatter.Label = "Humidity";
            humidityScatter.Color = Colors.Blue;
            humidityScatter.LineWidth = 2;
            humidityScatter.MarkerSize = 5;

            var refLine = plot.Add.HorizontalLine(50);
            refLine.Color = Colors.Gray.WithAlpha(0.5);
            refLine.LineWidth = 1;
            refLine.LinePattern = LinePattern.Dotted;

            plot.Axes.DateTimeTicksBottom();
            plot.XLabel("Date");
            plot.YLabel("Humidity (%)");
            plot.Title($"Humidity History - {SelectedLocation?.Name}");
            plot.Axes.SetLimitsY(0, 100);
            plot.Axes.Color(Colors.Gray);
            plot.Grid.MajorLineColor = Colors.LightGray;

            HumidityPlot = plot;
        }

        private void ClearCharts()
        {
            TemperaturePlot = new Plot();
            HumidityPlot = new Plot();
        }

        [RelayCommand]
        private async Task ExportToPdfAsync()
        {
            if (SelectedLocation == null || !WeatherHistory.Any())
            {
                ErrorMessage = "No data available to export";
                return;
            }

            await ExecuteAsync(async () =>
            {
                var pdfBytes = await _reportService.GeneratePdfReportAsync(
                    SelectedLocation.Id,
                    StartDate,
                    EndDate);

                var fileName = $"WeatherReport_{SelectedLocation.Name.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}.pdf";
                var filePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    fileName);

                await File.WriteAllBytesAsync(filePath, pdfBytes);

                SuccessMessage = $"Report saved to: {filePath}";
                OnPropertyChanged(nameof(HasSuccess));

                await Task.Delay(5000);
                SuccessMessage = string.Empty;
                OnPropertyChanged(nameof(HasSuccess));

            }, "Failed to export PDF report");
        }

        [RelayCommand]
        private async Task ExportToExcelAsync()
        {
            if (SelectedLocation == null || !WeatherHistory.Any())
            {
                ErrorMessage = "No data available to export";
                return;
            }

            await ExecuteAsync(async () =>
            {
                var excelBytes = await _reportService.GenerateExcelReportAsync(
                    SelectedLocation.Id,
                    StartDate,
                    EndDate);

                var fileName = $"WeatherReport_{SelectedLocation.Name.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}.xlsx";
                var filePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    fileName);

                await File.WriteAllBytesAsync(filePath, excelBytes);

                SuccessMessage = $"Report saved to: {filePath}";
                OnPropertyChanged(nameof(HasSuccess));

                await Task.Delay(5000);
                SuccessMessage = string.Empty;
                OnPropertyChanged(nameof(HasSuccess));

            }, "Failed to export Excel report");
        }

        partial void OnSelectedLocationChanged(SavedLocation? value)
        {
            // update global state with the new selection
            LocationService.SelectedLocation = value; 
            if (value != null && _isInitialized)
            {
                _ = LoadHistoryAsync();
            }
        }

        partial void OnStartDateChanged(DateTime value)
        {
            if (SelectedLocation != null && value <= EndDate)
            {
                _ = LoadHistoryAsync();
            }
        }

        partial void OnEndDateChanged(DateTime value)
        {
            if (SelectedLocation != null && value >= StartDate)
            {
                _ = LoadHistoryAsync();
            }
        }

        partial void OnWeatherHistoryChanged(List<WeatherRecord> value)
        {
            // Notify that statistics have changed
            OnPropertyChanged(nameof(AverageTemperature));
            OnPropertyChanged(nameof(MaxTemperature));
            OnPropertyChanged(nameof(MinTemperature));
            OnPropertyChanged(nameof(AverageHumidity));
        }

        private double CelsiusToFahrenheit(double celsius)
        {
            return (celsius * 9 / 5) + 32;
        }

        public double AverageTemperature => WeatherHistory.Any() ? WeatherHistory.Average(r => r.Temperature) : 0;
        public double MaxTemperature => WeatherHistory.Any() ? WeatherHistory.Max(r => r.Temperature) : 0;
        public double MinTemperature => WeatherHistory.Any() ? WeatherHistory.Min(r => r.Temperature) : 0;
        public double AverageHumidity => WeatherHistory.Any() ? WeatherHistory.Average(r => r.Humidity) : 0;
    }
}