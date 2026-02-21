using System;
using System.ComponentModel;
using WeatherDashboard.Data.Entities;

namespace WeatherDashboard.Services.Interfaces
{
    public interface IApplicationStateService : INotifyPropertyChanged
    {
        SavedLocation? SelectedLocation { get; set; }
        bool UseCelsius { get; set; }
    }
}
