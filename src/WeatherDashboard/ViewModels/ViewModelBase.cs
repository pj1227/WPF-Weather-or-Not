using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;
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

            StateService.PropertyChanged += StateService_PropertyChanged;
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

        private void StateService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(StateService.SelectedLocation))
            {
                OnPropertyChanged(nameof(SelectedLocation));
                OnSelectedLocationChanged();
            }

            if (e.PropertyName == nameof(StateService.UseCelsius))
            {
                OnPropertyChanged(nameof(UseCelsius));
                OnTemperatureUnitChanged();
            }
        }

        public virtual async Task InitializeAsync() { }

        protected virtual void OnSelectedLocationChanged() { }
        protected virtual void OnTemperatureUnitChanged() { }
    }
}