using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;
using WeatherDashboard.Data;
using WeatherDashboard.Services;
using WeatherDashboard.Services.Interfaces;
using WeatherDashboard.ViewModels;

namespace WeatherDashboard
{
    public partial class App : Application
    {
        public IServiceProvider? ServiceProvider { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            ServiceProvider = serviceCollection.BuildServiceProvider();

            InitializeDatabase();

            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Database Context
            services.AddDbContext<WeatherDbContext>(options =>
            {
                var dbPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "WeatherDashboard",
                    "weather.db");

                var directory = System.IO.Path.GetDirectoryName(dbPath);
                if (!string.IsNullOrEmpty(directory) && !System.IO.Directory.Exists(directory))
                {
                    System.IO.Directory.CreateDirectory(directory);
                }

                options.UseSqlite($"Data Source={dbPath}");
            });

            // Services
            services.AddSingleton<IWeatherService, WeatherApiService>();
            services.AddScoped<IDataService, DataService>();

            // HttpClient for weather API
            services.AddHttpClient<IWeatherService, WeatherApiService>(client =>
            {
                client.BaseAddress = new Uri("https://api.openweathermap.org/data/2.5/");
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            // ViewModels
            services.AddTransient<DashboardViewModel>();

            // Main Window
            services.AddSingleton<MainWindow>();
        }

        private void InitializeDatabase()
        {
            if (ServiceProvider == null) return;

            try
            {
                using var scope = ServiceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<WeatherDbContext>();
                dbContext.Database.Migrate();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to initialize database: {ex.Message}",
                    "Database Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}