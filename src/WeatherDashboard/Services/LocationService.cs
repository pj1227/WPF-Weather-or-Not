using System.ComponentModel;
using WeatherDashboard.Data.Entities;
using WeatherDashboard.Services.Interfaces;

namespace WeatherDashboard.Services
{
    public class LocationService : ILocationService
    {
        private SavedLocation? _selectedLocation;
        public event PropertyChangedEventHandler? PropertyChanged;

        public SavedLocation? SelectedLocation
        {
            get => _selectedLocation;
            set
            {
                if (!Equals(_selectedLocation, value))
                {
                    _selectedLocation = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedLocation)));
                }
            }
        }
    }
}
