using System.Windows.Controls;
using WeatherDashboard.ViewModels;

namespace WeatherDashboard.Views
{
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            InitializeComponent();
            Loaded += DashboardView_Loaded;
        }

        private async void DashboardView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is DashboardViewModel vm)
            {
                await vm.InitializeAsync();
            }
        }
    }
}