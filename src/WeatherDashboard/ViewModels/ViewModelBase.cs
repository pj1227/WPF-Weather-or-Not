using CommunityToolkit.Mvvm.ComponentModel;
using WeatherDashboard.Data.Entities;
using WeatherDashboard.Services.Interfaces;

namespace WeatherDashboard.ViewModels
{
    public abstract class ViewModelBase : ObservableObject
    {
        private bool _isBusy;
        private string _errorMessage = string.Empty;

        protected IDataService DataService { get; }
        protected IApplicationStateService StateService { get; }

        // Expose for binding - no backing field needed
        public SavedLocation? SelectedLocation
        {
            get => StateService.SelectedLocation;
            set
            {
                if (StateService.SelectedLocation != value)
                {
                    StateService.SelectedLocation = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool UseCelsius
        {
            get => StateService.UseCelsius;
            set
            {
                if (StateService.UseCelsius != value)
                {
                    StateService.UseCelsius = value;
                    OnPropertyChanged();
                }
            }
        }


        protected ViewModelBase(IDataService dataService, IApplicationStateService stateService)
        {
            DataService = dataService;
            StateService = stateService;

            StateService.SelectedLocationChanged += (s, location) =>
            {
                OnPropertyChanged(nameof(SelectedLocation));
            };

            StateService.TemperatureUnitChanged += (s, value) =>
            {
                OnPropertyChanged(nameof(UseCelsius));
            };
        }

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    OnPropertyChanged(nameof(IsNotBusy));
                }
            }
        }

        public bool IsNotBusy => !IsBusy;

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                if (SetProperty(ref _errorMessage, value))
                {
                    OnPropertyChanged(nameof(HasError));
                }
            }
        }

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        public void ClearMessages()
        {
            ErrorMessage = string.Empty;
        }

        protected async Task ExecuteAsync(Func<Task> operation, string errorMessage = "An error occurred")
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;
                ClearMessages();
                await operation();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"{errorMessage}: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        public virtual async Task InitializeAsync() { }
    }
}