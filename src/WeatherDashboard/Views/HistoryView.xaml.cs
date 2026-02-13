using System.Linq;
using System.Windows.Controls;
using WeatherDashboard.ViewModels;

namespace WeatherDashboard.Views
{
    public partial class HistoryView : UserControl
    {
        private HistoryViewModel? _viewModel;

        public HistoryView()
        {
            InitializeComponent();
            Loaded += HistoryView_Loaded;
        }

        private async void HistoryView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is HistoryViewModel vm)
            {
                _viewModel = vm;

                // Disable mouse wheel interactions initially to prevent crashes
                TemperatureChart.Interaction.Disable();
                HumidityChart.Interaction.Disable();

                // Subscribe to property changes FIRST
                vm.PropertyChanged += Vm_PropertyChanged;

                // Initialize (this will load data and trigger property changes)
                await vm.InitializeAsync();
            }
        }

        private void Vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (_viewModel == null) return;

            if (e.PropertyName == nameof(_viewModel.TemperaturePlot))
            {
                TemperatureChart.Reset(_viewModel.TemperaturePlot);
                TemperatureChart.Refresh();

                // Enable/disable interaction based on data
                if (_viewModel.WeatherHistory.Any())
                {
                    TemperatureChart.Interaction.Enable();
                }
                else
                {
                    TemperatureChart.Interaction.Disable();
                }
            }
            else if (e.PropertyName == nameof(_viewModel.HumidityPlot))
            {
                HumidityChart.Reset(_viewModel.HumidityPlot);
                HumidityChart.Refresh();

                // Enable/disable interaction based on data
                if (_viewModel.WeatherHistory.Any())
                {
                    HumidityChart.Interaction.Enable();
                }
                else
                {
                    HumidityChart.Interaction.Disable();
                }
            }
        }
    }
}