using System.Windows;
using WeatherDashboard.ViewModels;

namespace WeatherDashboard
{
    public partial class MainWindow : Window
    {
        public MainWindow(DashboardViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is DashboardViewModel vm)
            {
                await vm.InitializeAsync();
            }
        }
    }
}