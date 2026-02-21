using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Threading.Tasks;
using WeatherDashboard.Data.Entities;
using WeatherDashboard.Services.Interfaces;


namespace WeatherDashboard.Services
{
    public class ApplicationStateService : ObservableObject, IApplicationStateService
    {
        // backing properties
        private SavedLocation? _selectedLocation;
        private bool _useCelsius = true;

        // public state properties
        public SavedLocation? SelectedLocation
        {
            get => _selectedLocation;
            set => SetProperty(ref _selectedLocation, value);
        }
        public bool UseCelsius
        {
            get => _useCelsius;
            set => SetProperty(ref _useCelsius, value);
        }
    }
}

