using System.Collections.Generic;
using System.ComponentModel;
using Moq;
using WeatherDashboard.Data.Entities;
using WeatherDashboard.Services.Interfaces;

namespace WeatherDashboard.Tests.TestHelpers
{
    public static class MockSetup
    {
        public static Mock<IApplicationStateService> CreateStateService(bool useCelsius = true)
        {
            var mock = new Mock<IApplicationStateService>();
            mock.SetupProperty(s => s.UseCelsius, useCelsius);
            mock.SetupProperty(s => s.SelectedLocation, (SavedLocation?)null);
            return mock;
        }

        public static Mock<IDataService> CreateDataService()
        {
            var mock = new Mock<IDataService>();
            mock.Setup(d => d.GetSettingAsync(It.IsAny<string>(), It.IsAny<string?>()))
                .Returns((string _, string? def) => Task.FromResult<string?>(def));
            mock.Setup(d => d.GetAllLocationsAsync())
                .ReturnsAsync(new List<SavedLocation>());
            return mock;
        }

        public static Mock<IWeatherService> CreateWeatherService()
            => new Mock<IWeatherService>();

        /// <summary>
        /// Simulates ApplicationStateService.SetProperty firing PropertyChanged("UseCelsius"),
        /// which ViewModelBase.StateService_PropertyChanged receives and routes to
        /// OnTemperatureUnitChanged() on the derived ViewModel.
        /// </summary>
        public static void RaiseUnitChanged(Mock<IApplicationStateService> mockState, bool useCelsius)
        {
            mockState.Object.UseCelsius = useCelsius;
            mockState.Raise(
                s => s.PropertyChanged += null,
                new PropertyChangedEventArgs(nameof(IApplicationStateService.UseCelsius)));
        }
    }
}