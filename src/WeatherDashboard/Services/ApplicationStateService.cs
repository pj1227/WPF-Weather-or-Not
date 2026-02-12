using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Threading.Tasks;
using WeatherDashboard.Data.Entities;
using WeatherDashboard.Services.Interfaces;

namespace WeatherDashboard.Services
{
    namespace WeatherDashboard.Services
    {
        public class ApplicationStateService : IApplicationStateService
        {
            // backing properties
            private SavedLocation? _selectedLocation;
            private bool _useCelsius = true;

            // public state properties
            public SavedLocation? SelectedLocation
            {
                get => _selectedLocation;
                set
                {
                    if (_selectedLocation != value)
                    {
                        _selectedLocation = value;
                        SelectedLocationChanged?.Invoke(this, value);
                    }
                }
            }
            public bool UseCelsius
            {
                get => _useCelsius;
                set
                {
                    if (_useCelsius != value)
                    {
                        _useCelsius = value;
                        TemperatureUnitChanged?.Invoke(this, value);
                    }
                }
            }

            // event handlers 
            public event EventHandler<SavedLocation?>? SelectedLocationChanged;
            public event EventHandler<bool>? TemperatureUnitChanged;
        }
    }
}
