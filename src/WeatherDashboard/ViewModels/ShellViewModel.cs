using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WeatherDashboard.Services.Interfaces;

namespace WeatherDashboard.ViewModels
{
    public class ShellViewModel : ObservableObject
    {
        private readonly IApplicationStateService _stateService;

        public ShellViewModel(IApplicationStateService stateService)
        {
            _stateService = stateService;
        }

        public bool UseCelsius
        {
            get => _stateService.UseCelsius;
            set => _stateService.UseCelsius = value;
        }
    }

}
