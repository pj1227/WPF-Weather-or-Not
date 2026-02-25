using System.Collections.Generic;
using System.Linq;
using Moq;
using WeatherDashboard.Data.Entities;
using WeatherDashboard.Services.Interfaces;
using WeatherDashboard.Tests.TestHelpers;
using WeatherDashboard.ViewModels;
using Xunit;

namespace WeatherDashboard.Tests
{
    public class HistoryViewModelTests
    {
        private HistoryViewModel CreateVm(bool useCelsius,
            out Mock<IApplicationStateService> mockState)
        {
            mockState      = MockSetup.CreateStateService(useCelsius);
            var mockData   = MockSetup.CreateDataService();
            var mockReport = new Mock<IReportService>();
            return new HistoryViewModel(mockData.Object, mockState.Object, mockReport.Object);
        }

        private static List<WeatherRecord> SampleHistory() => new()
        {
            new WeatherRecord { Temperature = 0,  FeelsLike = -2, Humidity = 80,
                                Timestamp = System.DateTime.Now.AddDays(-2) },
            new WeatherRecord { Temperature = 10, FeelsLike = 8,  Humidity = 60,
                                Timestamp = System.DateTime.Now.AddDays(-1) },
            new WeatherRecord { Temperature = 20, FeelsLike = 19, Humidity = 40,
                                Timestamp = System.DateTime.Now },
        };

        // ── Stats in Celsius ─────────────────────────────────────────────────

        [Fact]
        public void AverageTemperature_Celsius_ReturnsCorrectAverage()
        {
            var vm = CreateVm(true, out _);
            vm.WeatherHistory = SampleHistory();
            Assert.Equal(10.0, vm.AverageTemperature, precision: 1);
        }

        [Fact]
        public void MaxTemperature_Celsius_ReturnsCorrectMax()
        {
            var vm = CreateVm(true, out _);
            vm.WeatherHistory = SampleHistory();
            Assert.Equal(20.0, vm.MaxTemperature, precision: 1);
        }

        [Fact]
        public void MinTemperature_Celsius_ReturnsCorrectMin()
        {
            var vm = CreateVm(true, out _);
            vm.WeatherHistory = SampleHistory();
            Assert.Equal(0.0, vm.MinTemperature, precision: 1);
        }

        [Fact]
        public void AverageHumidity_ReturnsCorrectAverage()
        {
            var vm = CreateVm(true, out _);
            vm.WeatherHistory = SampleHistory();
            Assert.Equal(60.0, vm.AverageHumidity, precision: 1);
        }

        // ── Stats convert on toggle ───────────────────────────────────────────

        [Fact]
        public void AverageTemperature_AfterToggleToFahrenheit_ConvertsCorrectly()
        {
            var vm = CreateVm(true, out var mockState);
            vm.WeatherHistory = SampleHistory();  // avg = 10°C = 50°F

            MockSetup.RaiseUnitChanged(mockState, false);

            Assert.Equal(50.0, vm.AverageTemperature, precision: 1);
        }

        [Fact]
        public void MaxTemperature_AfterToggleToFahrenheit_ConvertsCorrectly()
        {
            var vm = CreateVm(true, out var mockState);
            vm.WeatherHistory = SampleHistory();  // max = 20°C = 68°F

            MockSetup.RaiseUnitChanged(mockState, false);

            Assert.Equal(68.0, vm.MaxTemperature, precision: 1);
        }

        [Fact]
        public void MinTemperature_AfterToggleToFahrenheit_ConvertsCorrectly()
        {
            var vm = CreateVm(true, out var mockState);
            vm.WeatherHistory = SampleHistory();  // min = 0°C = 32°F

            MockSetup.RaiseUnitChanged(mockState, false);

            Assert.Equal(32.0, vm.MinTemperature, precision: 1);
        }

        [Fact]
        public void AverageTemperature_AfterToggleBackToCelsius_Reverts()
        {
            var vm = CreateVm(false, out var mockState);
            vm.WeatherHistory = SampleHistory();

            MockSetup.RaiseUnitChanged(mockState, true);

            Assert.Equal(10.0, vm.AverageTemperature, precision: 1);
        }

        // ── Empty history guard ───────────────────────────────────────────────

        [Fact]
        public void AverageTemperature_EmptyHistory_ReturnsZero()
        {
            var vm = CreateVm(true, out _);
            Assert.Equal(0.0, vm.AverageTemperature);
        }

        [Fact]
        public void MaxTemperature_EmptyHistory_ReturnsZero()
        {
            var vm = CreateVm(true, out _);
            Assert.Equal(0.0, vm.MaxTemperature);
        }

        [Fact]
        public void MinTemperature_EmptyHistory_ReturnsZero()
        {
            var vm = CreateVm(true, out _);
            Assert.Equal(0.0, vm.MinTemperature);
        }

        // ── PropertyChanged raised on toggle ──────────────────────────────────

        [Fact]
        public void ToggleUnit_WithHistory_RaisesPropertyChangedForAllStats()
        {
            var vm = CreateVm(true, out var mockState);
            vm.WeatherHistory = SampleHistory();

            var raised = new List<string?>();
            vm.PropertyChanged += (s, e) => raised.Add(e.PropertyName);

            MockSetup.RaiseUnitChanged(mockState, false);

            Assert.Contains(nameof(vm.AverageTemperature), raised);
            Assert.Contains(nameof(vm.MaxTemperature),     raised);
            Assert.Contains(nameof(vm.MinTemperature),     raised);
        }

        // ── Chart rebuild on toggle ───────────────────────────────────────────

        [Fact]
        public void ToggleUnit_WithHistory_RaisesPropertyChangedForTemperaturePlot()
        {
            var vm = CreateVm(true, out var mockState);
            vm.WeatherHistory = SampleHistory();

            var raised = new List<string?>();
            vm.PropertyChanged += (s, e) => raised.Add(e.PropertyName);

            MockSetup.RaiseUnitChanged(mockState, false);

            Assert.Contains(nameof(vm.TemperaturePlot), raised);
        }

        [Fact]
        public void ToggleUnit_WithNoHistory_StillRaisesTemperaturePlot()
        {
            // UpdateTemperatureChart always assigns TemperaturePlot (even when history is
            // empty it writes a new Plot()), so PropertyChanged fires regardless.
            var vm = CreateVm(true, out var mockState);

            var raised = new List<string?>();
            vm.PropertyChanged += (s, e) => raised.Add(e.PropertyName);

            MockSetup.RaiseUnitChanged(mockState, false);

            Assert.Contains(nameof(vm.TemperaturePlot), raised);
        }

        [Fact]
        public void ToggleUnit_DoesNotRaiseHumidityPlot()
        {
            // Humidity is unit-independent — HumidityPlot only rebuilds on data load,
            // not on unit toggle.
            var vm = CreateVm(true, out var mockState);
            vm.WeatherHistory = SampleHistory();

            var raised = new List<string?>();
            vm.PropertyChanged += (s, e) => raised.Add(e.PropertyName);

            MockSetup.RaiseUnitChanged(mockState, false);

            Assert.DoesNotContain(nameof(vm.HumidityPlot), raised);
        }
    }
}
