using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;
using System.Windows.Controls;
using WeatherDashboard.ViewModels;
using WeatherDashboard.Views;

namespace WeatherDashboard
{
    public partial class MainWindow : Window
    {
        private readonly IServiceProvider _serviceProvider;
        private bool _isInitialized = false;

        public MainWindow(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            InitializeComponent();
            _isInitialized = true;

            // Show dashboard after initialization
            ShowDashboard();
        }

        private void NavButton_Checked(object sender, RoutedEventArgs e)
        {
            // Guard against early calls during initialization
            if (!_isInitialized || _serviceProvider == null)
                return;

            if (sender == DashboardButton)
            {
                ShowDashboard();
            }
            else if (sender == HistoryButton)
            {
                ShowHistory();
            }
        }

        private void ShowDashboard()
        {
            if (_serviceProvider == null) return;

            var viewModel = _serviceProvider.GetRequiredService<DashboardViewModel>();
            var view = new DashboardView { DataContext = viewModel };

            ContentArea.Content = view;
        }

        private void ShowHistory()
        {
            if (_serviceProvider == null) return;

            var viewModel = _serviceProvider.GetRequiredService<HistoryViewModel>();
            var view = new HistoryView { DataContext = viewModel };

            ContentArea.Content = view;
        }
    }
}