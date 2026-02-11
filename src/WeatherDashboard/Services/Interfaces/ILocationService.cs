using System.ComponentModel;
using WeatherDashboard.Data.Entities;

namespace WeatherDashboard.Services.Interfaces
{
    public interface ILocationService : INotifyPropertyChanged
    {
        SavedLocation? SelectedLocation { get; set; }
    }
}
