using System;
using System.ComponentModel;
using WeatherDashboard.Data.Entities;

namespace WeatherDashboard.Services.Interfaces
{
    public interface IApplicationStateService
    {
        SavedLocation? SelectedLocation { get; set; }
        event EventHandler<SavedLocation?> SelectedLocationChanged;

        bool UseCelsius { get; set; }
        event EventHandler<bool> TemperatureUnitChanged;
    }

}
