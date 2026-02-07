# Weather Dashboard - Phase 2: ViewModels & Basic UI

## Overview
In Phase 2, we'll add the MVVM layer and create a basic working UI. At the end of this phase, you'll have a functioning weather dashboard that can search locations and display current weather.

**Estimated Time:** 6-8 hours  
**Prerequisites:** Phase 1 completed with 0 build errors  
**Goal:** Working dashboard that displays weather data

---

## Step 1: Add MVVM Foundation

### 1.1: ViewModels/ViewModelBase.cs

Create this file in the `ViewModels` folder:

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Threading.Tasks;

namespace WeatherDashboard.ViewModels
{
    public abstract class ViewModelBase : ObservableObject
    {
        private bool _isBusy;
        private string _errorMessage = string.Empty;

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
    }
}
```

---

## Step 2: Create DashboardViewModel

### ViewModels/DashboardViewModel.cs

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using WeatherDashboard.Data.Entities;
using WeatherDashboard.Models;
using WeatherDashboard.Services.Interfaces;

namespace WeatherDashboard.ViewModels
{
    public partial class DashboardViewModel : ViewModelBase
    {
        private readonly IWeatherService _weatherService;
        private readonly IDataService _dataService;

        [ObservableProperty]
        private WeatherData? _currentWeather;

        [ObservableProperty]
        private ObservableCollection<ForecastData> _forecast = new();

        [ObservableProperty]
        private SavedLocation? _selectedLocation;

        [ObservableProperty]
        private ObservableCollection<SavedLocation> _locations = new();

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private DateTime _lastUpdated;

        [ObservableProperty]
        private bool _useCelsius = true;

        public DashboardViewModel(IWeatherService weatherService, IDataService dataService)
        {
            _weatherService = weatherService;
            _dataService = dataService;
        }

        public async Task InitializeAsync()
        {
            await ExecuteAsync(async () =>
            {
                // Load temperature unit preference
                var tempUnit = await _dataService.GetSettingAsync("TemperatureUnit", "Celsius");
                UseCelsius = tempUnit == "Celsius";

                // Load API key from settings
                var apiKey = await _dataService.GetSettingAsync("ApiKey");
                if (!string.IsNullOrEmpty(apiKey))
                {
                    _weatherService.SetApiKey(apiKey);
                }

                // Load all locations
                var allLocations = await _dataService.GetAllLocationsAsync();
                Locations = new ObservableCollection<SavedLocation>(allLocations);

                // Load default location
                SelectedLocation = await _dataService.GetDefaultLocationAsync();

                if (SelectedLocation != null)
                {
                    await LoadWeatherAsync();
                }
            }, "Failed to initialize dashboard");
        }

        [RelayCommand]
        private async Task LoadWeatherAsync()
        {
            if (SelectedLocation == null)
            {
                ErrorMessage = "Please select a location first";
                return;
            }

            await ExecuteAsync(async () =>
            {
                // Get current weather
                CurrentWeather = await _weatherService.GetCurrentWeatherAsync(
                    SelectedLocation.Latitude,
                    SelectedLocation.Longitude);

                // Get forecast
                var forecastList = await _weatherService.GetForecastAsync(
                    SelectedLocation.Latitude,
                    SelectedLocation.Longitude);

                Forecast = new ObservableCollection<ForecastData>(forecastList);

                // Save to database
                var record = new WeatherRecord
                {
                    LocationId = SelectedLocation.Id,
                    Timestamp = DateTime.Now,
                    Temperature = CurrentWeather.Temperature,
                    FeelsLike = CurrentWeather.FeelsLike,
                    Humidity = CurrentWeather.Humidity,
                    Pressure = CurrentWeather.Pressure,
                    WindSpeed = CurrentWeather.WindSpeed,
                    Description = CurrentWeather.Description,
                    IconCode = CurrentWeather.IconCode
                };

                await _dataService.SaveWeatherRecordAsync(record);

                LastUpdated = DateTime.Now;

            }, "Failed to load weather data");
        }

        [RelayCommand(CanExecute = nameof(CanSearch))]
        private async Task SearchLocationAsync()
        {
            await ExecuteAsync(async () =>
            {
                // Get weather for the search text
                var weather = await _weatherService.GetCurrentWeatherAsync(SearchText);

                // Check if location already exists
                var existingLocation = await _dataService.GetLocationByNameAsync(weather.LocationName);
                
                if (existingLocation != null)
                {
                    SelectedLocation = existingLocation;
                }
                else
                {
                    // Create new location
                    var newLocation = new SavedLocation
                    {
                        Name = weather.LocationName,
                        Latitude = weather.Latitude,
                        Longitude = weather.Longitude,
                        Country = weather.Country,
                        CreatedDate = DateTime.Now,
                        IsFavorite = false
                    };

                    SelectedLocation = await _dataService.AddLocationAsync(newLocation);
                    
                    // Reload locations
                    var allLocations = await _dataService.GetAllLocationsAsync();
                    Locations = new ObservableCollection<SavedLocation>(allLocations);
                }

                // Load weather
                await LoadWeatherAsync();
                
                // Clear search
                SearchText = string.Empty;

            }, "Location not found. Please check the spelling and try again");
        }

        private bool CanSearch() => !string.IsNullOrWhiteSpace(SearchText) && !IsBusy;

        [RelayCommand]
        private async Task RefreshAsync()
        {
            await LoadWeatherAsync();
        }

        partial void OnSelectedLocationChanged(SavedLocation? value)
        {
            if (value != null && CurrentWeather?.LocationName != value.Name)
            {
                _ = LoadWeatherAsync();
            }
        }

        public string GetFormattedTemperature(double temp)
        {
            if (UseCelsius)
                return $"{temp:F1}°C";
            else
            {
                var fahrenheit = (temp * 9 / 5) + 32;
                return $"{fahrenheit:F1}°F";
            }
        }
    }
}
```

---

## Step 3: Create MainWindow UI

### MainWindow.xaml

Replace the entire content:

```xml
<Window x:Class="WeatherDashboard.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        mc:Ignorable="d"
        Title="Weather Dashboard" 
        Height="700" 
        Width="1000"
        WindowStartupLocation="CenterScreen"
        Background="#F5F5F5">
    
    <Window.Resources>
        <!-- Converters -->
        <BooleanToVisibilityConverter x:Key="BoolToVisibilityConverter"/>
        
        <!-- Styles -->
        <Style x:Key="CardStyle" TargetType="Border">
            <Setter Property="Background" Value="White"/>
            <Setter Property="CornerRadius" Value="8"/>
            <Setter Property="Padding" Value="20"/>
            <Setter Property="Margin" Value="10"/>
            <Setter Property="Effect">
                <Setter.Value>
                    <DropShadowEffect Color="Black" Opacity="0.1" BlurRadius="10" ShadowDepth="2"/>
                </Setter.Value>
            </Setter>
        </Style>

        <Style x:Key="ButtonStyle" TargetType="Button">
            <Setter Property="Padding" Value="20,10"/>
            <Setter Property="Background" Value="#3498db"/>
            <Setter Property="Foreground" Value="White"/>
            <Setter Property="BorderThickness" Value="0"/>
            <Setter Property="Cursor" Value="Hand"/>
            <Setter Property="FontSize" Value="14"/>
            <Setter Property="Template">
                <Setter.Value>
                    <ControlTemplate TargetType="Button">
                        <Border Background="{TemplateBinding Background}"
                                CornerRadius="4"
                                Padding="{TemplateBinding Padding}">
                            <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
                        </Border>
                    </ControlTemplate>
                </Setter.Value>
            </Setter>
            <Style.Triggers>
                <Trigger Property="IsMouseOver" Value="True">
                    <Setter Property="Background" Value="#2980b9"/>
                </Trigger>
                <Trigger Property="IsEnabled" Value="False">
                    <Setter Property="Background" Value="#95a5a6"/>
                    <Setter Property="Cursor" Value="Arrow"/>
                </Trigger>
            </Style.Triggers>
        </Style>

        <Style x:Key="TextBoxStyle" TargetType="TextBox">
            <Setter Property="Padding" Value="10,8"/>
            <Setter Property="FontSize" Value="14"/>
            <Setter Property="BorderBrush" Value="#BDC3C7"/>
            <Setter Property="BorderThickness" Value="1"/>
        </Style>
    </Window.Resources>

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <!-- Header -->
        <Border Grid.Row="0" Background="#2C3E50" Padding="20">
            <TextBlock Text="Weather Dashboard" 
                       FontSize="24" 
                       FontWeight="Bold" 
                       Foreground="White"/>
        </Border>

        <!-- Search Bar -->
        <Border Grid.Row="1" Background="White" Padding="20" BorderBrush="#ECF0F1" BorderThickness="0,0,0,1">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                    <ColumnDefinition Width="Auto"/>
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>

                <TextBox Grid.Column="0" 
                         x:Name="SearchBox"
                         Text="{Binding SearchText, UpdateSourceTrigger=PropertyChanged}"
                         Style="{StaticResource TextBoxStyle}"
                         VerticalAlignment="Center"
                         Margin="0,0,10,0">
                    <TextBox.InputBindings>
                        <KeyBinding Key="Enter" Command="{Binding SearchLocationCommand}"/>
                    </TextBox.InputBindings>
                </TextBox>

                <Button Grid.Column="1" 
                        Content="Search Location" 
                        Command="{Binding SearchLocationCommand}"
                        Style="{StaticResource ButtonStyle}"
                        Margin="0,0,10,0"/>

                <ComboBox Grid.Column="2"
                          ItemsSource="{Binding Locations}"
                          SelectedItem="{Binding SelectedLocation}"
                          DisplayMemberPath="Name"
                          Width="200"
                          Height="38"
                          FontSize="14"
                          Margin="0,0,10,0"
                          VerticalContentAlignment="Center"/>

                <Button Grid.Column="3" 
                        Content="Refresh" 
                        Command="{Binding RefreshCommand}"
                        Style="{StaticResource ButtonStyle}"/>
            </Grid>
        </Border>

        <!-- Main Content -->
        <ScrollViewer Grid.Row="2" VerticalScrollBarVisibility="Auto">
            <Grid Margin="20">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="2*"/>
                    <ColumnDefinition Width="3*"/>
                </Grid.ColumnDefinitions>

                <!-- Current Weather Card -->
                <Border Grid.Column="0" Style="{StaticResource CardStyle}">
                    <StackPanel>
                        <TextBlock Text="{Binding CurrentWeather.LocationName, FallbackValue='No Location'}" 
                                   FontSize="28" 
                                   FontWeight="Bold"
                                   TextAlignment="Center"/>
                        
                        <TextBlock Text="{Binding CurrentWeather.Country, FallbackValue=''}" 
                                   FontSize="16" 
                                   Foreground="Gray"
                                   TextAlignment="Center"
                                   Margin="0,5,0,0"/>

                        <TextBlock Text="{Binding LastUpdated, StringFormat='Last updated: {0:hh:mm tt}', FallbackValue=''}" 
                                   FontSize="12" 
                                   Foreground="Gray"
                                   TextAlignment="Center"
                                   Margin="0,5,0,30"/>

                        <TextBlock Text="{Binding CurrentWeather.Temperature, StringFormat='{}{0:F1}°', FallbackValue='--°'}" 
                                   FontSize="72" 
                                   FontWeight="Bold"
                                   HorizontalAlignment="Center"
                                   Foreground="#3498db"/>

                        <TextBlock Text="{Binding CurrentWeather.Description, FallbackValue='No data'}" 
                                   FontSize="20" 
                                   HorizontalAlignment="Center"
                                   Margin="0,10,0,30"
                                   TextTransform="Capitalize"/>

                        <!-- Weather Details -->
                        <Grid Margin="0,10">
                            <Grid.ColumnDefinitions>
                                <ColumnDefinition Width="*"/>
                                <ColumnDefinition Width="*"/>
                            </Grid.ColumnDefinitions>
                            <TextBlock Grid.Column="0" Text="Feels Like" Foreground="Gray"/>
                            <TextBlock Grid.Column="1" 
                                       Text="{Binding CurrentWeather.FeelsLike, StringFormat='{}{0:F1}°', FallbackValue='--°'}" 
                                       HorizontalAlignment="Right" 
                                       FontWeight="SemiBold"/>
                        </Grid>

                        <Grid Margin="0,10">
                            <Grid.ColumnDefinitions>
                                <ColumnDefinition Width="*"/>
                                <ColumnDefinition Width="*"/>
                            </Grid.ColumnDefinitions>
                            <TextBlock Grid.Column="0" Text="Humidity" Foreground="Gray"/>
                            <TextBlock Grid.Column="1" 
                                       Text="{Binding CurrentWeather.Humidity, StringFormat='{}{0:F0}%', FallbackValue='--'}" 
                                       HorizontalAlignment="Right" 
                                       FontWeight="SemiBold"/>
                        </Grid>

                        <Grid Margin="0,10">
                            <Grid.ColumnDefinitions>
                                <ColumnDefinition Width="*"/>
                                <ColumnDefinition Width="*"/>
                            </Grid.ColumnDefinitions>
                            <TextBlock Grid.Column="0" Text="Wind Speed" Foreground="Gray"/>
                            <TextBlock Grid.Column="1" 
                                       Text="{Binding CurrentWeather.WindSpeed, StringFormat='{}{0:F1} m/s', FallbackValue='--'}" 
                                       HorizontalAlignment="Right" 
                                       FontWeight="SemiBold"/>
                        </Grid>

                        <Grid Margin="0,10">
                            <Grid.ColumnDefinitions>
                                <ColumnDefinition Width="*"/>
                                <ColumnDefinition Width="*"/>
                            </Grid.ColumnDefinitions>
                            <TextBlock Grid.Column="0" Text="Pressure" Foreground="Gray"/>
                            <TextBlock Grid.Column="1" 
                                       Text="{Binding CurrentWeather.Pressure, StringFormat='{}{0:F0} hPa', FallbackValue='--'}" 
                                       HorizontalAlignment="Right" 
                                       FontWeight="SemiBold"/>
                        </Grid>
                    </StackPanel>
                </Border>

                <!-- Forecast Card -->
                <Border Grid.Column="1" Style="{StaticResource CardStyle}">
                    <StackPanel>
                        <TextBlock Text="5-Day Forecast" 
                                   FontSize="24" 
                                   FontWeight="Bold"
                                   Margin="0,0,0,20"/>

                        <ItemsControl ItemsSource="{Binding Forecast}">
                            <ItemsControl.ItemTemplate>
                                <DataTemplate>
                                    <Border Background="#F8F9FA" 
                                            CornerRadius="8" 
                                            Padding="15" 
                                            Margin="0,0,0,10">
                                        <Grid>
                                            <Grid.ColumnDefinitions>
                                                <ColumnDefinition Width="2*"/>
                                                <ColumnDefinition Width="3*"/>
                                                <ColumnDefinition Width="2*"/>
                                            </Grid.ColumnDefinitions>

                                            <TextBlock Grid.Column="0" 
                                                       Text="{Binding Date, StringFormat='{}{0:ddd, MMM d}'}"
                                                       VerticalAlignment="Center"
                                                       FontWeight="SemiBold"/>

                                            <TextBlock Grid.Column="1" 
                                                       Text="{Binding Description}"
                                                       VerticalAlignment="Center"
                                                       TextWrapping="Wrap"
                                                       Margin="10,0"/>

                                            <StackPanel Grid.Column="2" 
                                                        Orientation="Horizontal" 
                                                        HorizontalAlignment="Right"
                                                        VerticalAlignment="Center">
                                                <TextBlock Text="{Binding TempMax, StringFormat='{}{0:F0}°'}" 
                                                           FontWeight="Bold"
                                                           FontSize="16"/>
                                                <TextBlock Text=" / " 
                                                           Foreground="Gray"
                                                           Margin="5,0"/>
                                                <TextBlock Text="{Binding TempMin, StringFormat='{}{0:F0}°'}" 
                                                           Foreground="Gray"
                                                           FontSize="16"/>
                                            </StackPanel>
                                        </Grid>
                                    </Border>
                                </DataTemplate>
                            </ItemsControl.ItemTemplate>
                        </ItemsControl>
                    </StackPanel>
                </Border>
            </Grid>
        </ScrollViewer>

        <!-- Loading Overlay -->
        <Grid Grid.RowSpan="3" 
              Background="#80000000" 
              Visibility="{Binding IsBusy, Converter={StaticResource BoolToVisibilityConverter}}">
            <StackPanel HorizontalAlignment="Center" VerticalAlignment="Center">
                <ProgressBar IsIndeterminate="True" 
                             Width="200" 
                             Height="4"
                             Margin="0,0,0,15"/>
                <TextBlock Text="Loading..." 
                           Foreground="White" 
                           FontSize="16"/>
            </StackPanel>
        </Grid>

        <!-- Error Message -->
        <Border Grid.Row="2"
                Background="#E74C3C" 
                Padding="15"
                VerticalAlignment="Top"
                Margin="20,20,20,0"
                Visibility="{Binding HasError, Converter={StaticResource BoolToVisibilityConverter}}">
            <StackPanel Orientation="Horizontal">
                <TextBlock Text="⚠ " 
                           Foreground="White" 
                           FontSize="16" 
                           FontWeight="Bold"/>
                <TextBlock Text="{Binding ErrorMessage}" 
                           Foreground="White" 
                           FontSize="14"
                           TextWrapping="Wrap"/>
            </StackPanel>
        </Border>
    </Grid>
</Window>
```

---

## Step 4: Update MainWindow Code-Behind

### MainWindow.xaml.cs

Replace the entire content:

```csharp
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
```

---

## Step 5: Update App.xaml.cs

Update the `ConfigureServices` method to include ViewModels:

```csharp
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
```

---

## Step 6: Build (Checkpoint 1)

```powershell
dotnet build
```

**Expected Result:** Build succeeded with 0 errors.

---

## Step 7: Get OpenWeatherMap API Key

1. Go to https://openweathermap.org/api
2. Sign up for free account
3. Navigate to API Keys
4. Copy your API key

---

## Step 8: Add API Key to Database

Since we don't have a settings UI yet, we'll add the API key directly to the database:

### Option A: Using DB Browser for SQLite
1. Download DB Browser for SQLite (free)
2. Open `%LOCALAPPDATA%\WeatherDashboard\weather.db`
3. Go to "Browse Data" tab
4. Select `UserSettings` table
5. Click "New Record"
6. Fill in:
   - Key: `ApiKey`
   - Value: `YOUR_API_KEY_HERE`
   - LastModified: `2024-02-07 12:00:00`
7. Click "Write Changes"

### Option B: Using SQL
In Package Manager Console or a SQL tool:
```sql
INSERT INTO UserSettings (Key, Value, LastModified) 
VALUES ('ApiKey', 'YOUR_API_KEY_HERE', datetime('now'));
```

---

## Step 9: Run the Application!

```powershell
dotnet run
```

Or press **F5** in Visual Studio.

---

## Step 10: Test the Application

### Test 1: Search for a Location
1. Type "London" in the search box
2. Click "Search Location"
3. Wait for loading
4. Verify weather data appears

### Test 2: Save Another Location
1. Search for "New York"
2. Verify it's added to the dropdown
3. Switch between locations using dropdown

### Test 3: Refresh
1. Click "Refresh" button
2. Verify data updates
3. Check "Last updated" timestamp changes

---

## Phase 2 Completion Checklist

- [ ] ViewModelBase created
- [ ] DashboardViewModel created
- [ ] MainWindow UI updated
- [ ] MainWindow code-behind updated
- [ ] App.xaml.cs updated with ViewModels
- [ ] API key added to database
- [ ] Application builds with 0 errors
- [ ] Application runs successfully
- [ ] Can search for locations
- [ ] Weather data displays correctly
- [ ] Forecast shows 5 days
- [ ] Can switch between saved locations

---

## What You Have Now

✅ Complete MVVM architecture  
✅ Working weather dashboard UI  
✅ Location search functionality  
✅ Current weather display  
✅ 5-day forecast  
✅ Location management (basic)  
✅ Data persistence

---

## Common Issues & Solutions

### Issue: "Cannot find API key"
**Solution:** Make sure you added the API key to the database in Step 8.

### Issue: "No data displays"
**Solution:** 
1. Check your API key is valid
2. Check internet connection
3. Look for errors in the error message banner

### Issue: Search doesn't work
**Solution:** Make sure you have internet and a valid API key.

### Issue: Build errors about nullable references
**Solution:** The code uses nullable reference types. Make sure your `.csproj` has `<Nullable>enable</Nullable>`.

---

## Files Created/Modified in Phase 2

**New Files:**
```
ViewModels/
├── ViewModelBase.cs
└── DashboardViewModel.cs
```

**Modified Files:**
```
MainWindow.xaml (completely replaced)
MainWindow.xaml.cs (completely replaced)
App.xaml.cs (ConfigureServices updated)
```

**Total New Lines of Code:** ~550

---

## Next Phase Preview

**Phase 3** will add:
- History view with charts (ScottPlot)
- Reporting (PDF/Excel export)
- Advanced location management
- Settings page

---

## Congratulations! 🎉

You now have a working weather dashboard! You can:
- ✅ Search for any city in the world
- ✅ See current weather conditions
- ✅ View 5-day forecast
- ✅ Save multiple locations
- ✅ Switch between saved locations
- ✅ See data persist in database

**Take a moment to test everything before moving to Phase 3!**
