using System.Collections.Generic;
using System.Collections.ObjectModel;
using Moq;
using WeatherDashboard.Models;
using WeatherDashboard.Services.Interfaces;
using WeatherDashboard.Tests.TestHelpers;
using WeatherDashboard.ViewModels;
using Xunit;

namespace WeatherDashboard.Tests
{
    public class DashboardViewModelTests
    {
        private DashboardViewModel CreateVm(bool useCelsius,
            out Mock<IApplicationStateService> mockState)
        {
            mockState       = MockSetup.CreateStateService(useCelsius);
            var mockData    = MockSetup.CreateDataService();
            var mockWeather = MockSetup.CreateWeatherService();
            return new DashboardViewModel(mockData.Object, mockState.Object, mockWeather.Object);
        }

        // ── FormattedTemperature — null guard ────────────────────────────────

        [Fact]
        public void FormattedTemperature_NullWeather_ReturnsDash()
        {
            var vm = CreateVm(true, out _);
            Assert.Equal("--\u00b0", vm.FormattedTemperature);
        }

        [Fact]
        public void FormattedFeelsLike_NullWeather_ReturnsDash()
        {
            var vm = CreateVm(true, out _);
            Assert.Equal("--\u00b0", vm.FormattedFeelsLike);
        }

        // ── FormattedTemperature — Celsius ───────────────────────────────────

        [Fact]
        public void FormattedTemperature_Celsius_FormatsCorrectly()
        {
            var vm = CreateVm(true, out _);
            vm.CurrentWeather = new WeatherData { Temperature = 20.0, FeelsLike = 18.0 };
            Assert.Equal("20.0\u00b0C", vm.FormattedTemperature);
        }

        [Fact]
        public void FormattedFeelsLike_Celsius_FormatsCorrectly()
        {
            var vm = CreateVm(true, out _);
            vm.CurrentWeather = new WeatherData { Temperature = 20.0, FeelsLike = 18.0 };
            Assert.Equal("18.0\u00b0C", vm.FormattedFeelsLike);
        }

        // ── FormattedTemperature — Fahrenheit ────────────────────────────────

        [Fact]
        public void FormattedTemperature_Fahrenheit_ConvertsAndFormats()
        {
            var vm = CreateVm(false, out _);
            vm.CurrentWeather = new WeatherData { Temperature = 0.0, FeelsLike = -5.0 };
            Assert.Equal("32.0\u00b0F", vm.FormattedTemperature);
        }

        [Fact]
        public void FormattedFeelsLike_Fahrenheit_ConvertsAndFormats()
        {
            var vm = CreateVm(false, out _);
            vm.CurrentWeather = new WeatherData { Temperature = 0.0, FeelsLike = -5.0 };
            // -5°C = 23°F
            Assert.Equal("23.0\u00b0F", vm.FormattedFeelsLike);
        }

        // ── Unit toggle — formatted strings update ───────────────────────────

        [Fact]
        public void ToggleToFahrenheit_FormattedTemperature_Updates()
        {
            var vm = CreateVm(true, out var mockState);
            vm.CurrentWeather = new WeatherData { Temperature = 100.0, FeelsLike = 95.0 };

            MockSetup.RaiseUnitChanged(mockState, false);

            Assert.Equal("212.0\u00b0F", vm.FormattedTemperature);
        }

        [Fact]
        public void ToggleBackToCelsius_FormattedTemperature_Reverts()
        {
            var vm = CreateVm(false, out var mockState);
            vm.CurrentWeather = new WeatherData { Temperature = 100.0, FeelsLike = 95.0 };

            MockSetup.RaiseUnitChanged(mockState, true);

            Assert.Equal("100.0\u00b0C", vm.FormattedTemperature);
        }

        [Fact]
        public void ToggleToFahrenheit_FormattedFeelsLike_Updates()
        {
            var vm = CreateVm(true, out var mockState);
            vm.CurrentWeather = new WeatherData { Temperature = 0.0, FeelsLike = -5.0 };

            MockSetup.RaiseUnitChanged(mockState, false);

            Assert.Equal("23.0\u00b0F", vm.FormattedFeelsLike);
        }

        // ── Unit toggle — PropertyChanged raised ─────────────────────────────

        [Fact]
        public void ToggleUnit_RaisesPropertyChangedForFormattedTemperature()
        {
            var vm = CreateVm(true, out var mockState);
            vm.CurrentWeather = new WeatherData { Temperature = 20.0, FeelsLike = 18.0 };

            var raised = new List<string?>();
            vm.PropertyChanged += (s, e) => raised.Add(e.PropertyName);

            MockSetup.RaiseUnitChanged(mockState, false);

            Assert.Contains(nameof(vm.FormattedTemperature), raised);
            Assert.Contains(nameof(vm.FormattedFeelsLike), raised);
        }

        // ── Forecast TempDisplay stamping ────────────────────────────────────

        [Fact]
        public void ToggleToFahrenheit_ForecastItems_TempMaxDisplay_Updates()
        {
            var vm = CreateVm(true, out var mockState);
            vm.Forecast = new ObservableCollection<ForecastData>
            {
                new ForecastData { TempMax = 0.0, TempMin = -10.0,
                                   TempMaxDisplay = 0.0, TempMinDisplay = -10.0 }
            };

            MockSetup.RaiseUnitChanged(mockState, false);

            // 0°C = 32°F, -10°C = 14°F
            Assert.Equal(32.0, vm.Forecast[0].TempMaxDisplay, precision: 1);
            Assert.Equal(14.0, vm.Forecast[0].TempMinDisplay, precision: 1);
        }

        [Fact]
        public void ToggleBackToCelsius_ForecastItems_TempMaxDisplay_Reverts()
        {
            var vm = CreateVm(false, out var mockState);
            vm.Forecast = new ObservableCollection<ForecastData>
            {
                new ForecastData { TempMax = 0.0, TempMin = -10.0,
                                   TempMaxDisplay = 32.0, TempMinDisplay = 14.0 }
            };

            MockSetup.RaiseUnitChanged(mockState, true);

            Assert.Equal(0.0,   vm.Forecast[0].TempMaxDisplay, precision: 1);
            Assert.Equal(-10.0, vm.Forecast[0].TempMinDisplay, precision: 1);
        }

        [Fact]
        public void ToggleUnit_EmptyForecast_DoesNotThrow()
        {
            var vm = CreateVm(true, out var mockState);
            vm.Forecast = new ObservableCollection<ForecastData>();

            var ex = Record.Exception(() => MockSetup.RaiseUnitChanged(mockState, false));
            Assert.Null(ex);
        }
    }
}
