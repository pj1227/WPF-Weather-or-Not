using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;
using WeatherDashboard.Services.Interfaces;

namespace WeatherDashboard.ViewModels
{
    public class ShellViewModel : ObservableObject
    {
        private readonly IApplicationStateService _stateService;

        public ShellViewModel(IApplicationStateService stateService)
        {
            _stateService = stateService;

            // When App.OnStartup loads the saved temperature unit from the database
            // and sets it on the service, we need to notify the ToggleButton binding
            // so it reflects the loaded value rather than the default.
            _stateService.PropertyChanged += StateService_PropertyChanged;
        }

        public bool UseCelsius
        {
            get => _stateService.UseCelsius;
            set => _stateService.UseCelsius = value;
        }

        private void StateService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IApplicationStateService.UseCelsius))
                OnPropertyChanged(nameof(UseCelsius));
        }
    }
}