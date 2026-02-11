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
        protected ILocationService LocationService { get; }

        public SavedLocation? SelectedLocation
        {
            get => LocationService.SelectedLocation;
            set => LocationService.SelectedLocation = value;
        }

        protected ViewModelBase(IDataService dataService, ILocationService locationService)
        {
            DataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
            LocationService = locationService ?? throw new ArgumentNullException(nameof(locationService));

            // Subscribe to location changes
            LocationService.PropertyChanged += (_, __) => OnPropertyChanged(nameof(SelectedLocation));
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

        public virtual async Task InitializeAsync()
        {
            // set default location
            if (SelectedLocation == null)
                SelectedLocation = await DataService.GetDefaultLocationAsync();
        }
    }
}