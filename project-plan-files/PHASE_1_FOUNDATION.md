# Weather Dashboard - Phase 1: Foundation

## Overview
In Phase 1, we'll set up the project structure, database, and core services. At the end of this phase, you'll have a working foundation ready for UI development.

**Estimated Time:** 4-6 hours  
**Goal:** Working database and API integration with no build errors

---

## Prerequisites Checklist

Before starting:
- [ ] Visual Studio 2022 installed
- [ ] .NET 8 SDK installed
- [ ] NuGet.org configured as package source
- [ ] Internet connection active

---

## Step 1: Create the Project

### Using Visual Studio:
1. File → New → Project
2. Select "WPF Application"
3. Project Name: `WeatherDashboard`
4. Location: Choose your location
5. Framework: **.NET 8.0**
6. Click Create

### Verify Your .csproj File:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <UseWPF>true</UseWPF>
  </PropertyGroup>
</Project>
```

---

## Step 2: Configure NuGet Sources

### Option A: Command Line (Recommended)
```powershell
dotnet nuget add source https://api.nuget.org/v3/index.json -n nuget.org
```

### Option B: Visual Studio
1. Tools → Options → NuGet Package Manager → Package Sources
2. Ensure **nuget.org** is listed and checked
3. URL should be: `https://api.nuget.org/v3/index.json`

---

## Step 3: Install NuGet Packages

Run these commands one at a time in the Package Manager Console or terminal:

```powershell
# Entity Framework Core
dotnet add package Microsoft.EntityFrameworkCore --version 8.0.11
dotnet add package Microsoft.EntityFrameworkCore.Sqlite --version 8.0.11
dotnet add package Microsoft.EntityFrameworkCore.Tools --version 8.0.11

# Dependency Injection
dotnet add package Microsoft.Extensions.DependencyInjection --version 8.0.1
dotnet add package Microsoft.Extensions.Http --version 8.0.1

# MVVM Toolkit
dotnet add package CommunityToolkit.Mvvm --version 8.2.2
```

### Verify Installation:
```powershell
dotnet list package
```

You should see all 6 packages listed.

---

## Step 4: Create Project Folders

Right-click on your project in Solution Explorer and create these folders:

```
WeatherDashboard/
├── Data/
│   └── Entities/
├── Models/
├── Services/
│   └── Interfaces/
└── ViewModels/
```

### PowerShell Command (if preferred):
```powershell
New-Item -ItemType Directory -Force -Path Data/Entities, Models, Services/Interfaces, ViewModels
```

---

## Step 5: Add Phase 1 Code Files

Add files in this exact order to avoid build errors:

### 5.1: Data/Entities/WeatherRecord.cs
```csharp
using System;

namespace WeatherDashboard.Data.Entities
{
    public class WeatherRecord
    {
        public int Id { get; set; }
        public int LocationId { get; set; }
        public DateTime Timestamp { get; set; }
        public double Temperature { get; set; }
        public double FeelsLike { get; set; }
        public double Humidity { get; set; }
        public double Pressure { get; set; }
        public double WindSpeed { get; set; }
        public string Description { get; set; } = string.Empty;
        public string IconCode { get; set; } = string.Empty;
        
        // Navigation property
        public SavedLocation? Location { get; set; }
    }
}
```

### 5.2: Data/Entities/SavedLocation.cs
```csharp
using System;
using System.Collections.Generic;

namespace WeatherDashboard.Data.Entities
{
    public class SavedLocation
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Country { get; set; } = string.Empty;
        public bool IsFavorite { get; set; }
        public DateTime CreatedDate { get; set; }
        
        // Navigation property
        public ICollection<WeatherRecord> WeatherRecords { get; set; } = new List<WeatherRecord>();
    }
}
```

### 5.3: Data/Entities/UserSetting.cs
```csharp
using System;

namespace WeatherDashboard.Data.Entities
{
    public class UserSetting
    {
        public int Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public DateTime LastModified { get; set; }
    }
}
```

### 5.4: Data/WeatherDbContext.cs
```csharp
using Microsoft.EntityFrameworkCore;
using WeatherDashboard.Data.Entities;

namespace WeatherDashboard.Data
{
    public class WeatherDbContext : DbContext
    {
        public WeatherDbContext(DbContextOptions<WeatherDbContext> options) 
            : base(options)
        {
        }

        public DbSet<WeatherRecord> WeatherRecords { get; set; }
        public DbSet<SavedLocation> SavedLocations { get; set; }
        public DbSet<UserSetting> UserSettings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure WeatherRecord
            modelBuilder.Entity<WeatherRecord>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Temperature).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(200);
                entity.Property(e => e.IconCode).HasMaxLength(10);

                entity.HasIndex(e => new { e.LocationId, e.Timestamp });

                entity.HasOne(e => e.Location)
                    .WithMany(l => l.WeatherRecords)
                    .HasForeignKey(e => e.LocationId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure SavedLocation
            modelBuilder.Entity<SavedLocation>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Country).HasMaxLength(100);
                entity.HasIndex(e => e.Name);
            });

            // Configure UserSetting
            modelBuilder.Entity<UserSetting>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Key).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Value).HasMaxLength(500);
                entity.HasIndex(e => e.Key).IsUnique();
            });

            // Seed default settings
            modelBuilder.Entity<UserSetting>().HasData(
                new UserSetting 
                { 
                    Id = 1, 
                    Key = "TemperatureUnit", 
                    Value = "Celsius",
                    LastModified = DateTime.Now
                },
                new UserSetting 
                { 
                    Id = 2, 
                    Key = "RefreshInterval", 
                    Value = "30",
                    LastModified = DateTime.Now
                }
            );
        }
    }
}
```

### 5.5: Data/WeatherDbContextFactory.cs
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;
using System.IO;

namespace WeatherDashboard.Data
{
    public class WeatherDbContextFactory : IDesignTimeDbContextFactory<WeatherDbContext>
    {
        public WeatherDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<WeatherDbContext>();

            var dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "WeatherDashboard",
                "weather.db");

            var directory = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            optionsBuilder.UseSqlite($"Data Source={dbPath}");

            return new WeatherDbContext(optionsBuilder.Options);
        }
    }
}
```

---

## Step 6: Build to Verify (Checkpoint 1)

```powershell
dotnet build
```

**Expected Result:** Build succeeded with 0 errors.

If you have errors, stop here and resolve them before continuing.

---

## Step 7: Create Database Migration

### Install EF Core Tools (if needed):
```powershell
dotnet tool install --global dotnet-ef --version 8.0.11
```

### Create Initial Migration:
```powershell
dotnet ef migrations add InitialCreate
```

**Expected Result:** 
- New folder created: `Data/Migrations/`
- Migration file created with timestamp
- Message: "Done."

### Apply Migration:
```powershell
dotnet ef database update
```

**Expected Result:**
- Message: "Applying migration..."
- Message: "Done."
- Database created at: `%LOCALAPPDATA%\WeatherDashboard\weather.db`

---

## Step 8: Add Domain Models

### 8.1: Models/WeatherData.cs
```csharp
using System;

namespace WeatherDashboard.Models
{
    public class WeatherData
    {
        public string LocationName { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Temperature { get; set; }
        public double FeelsLike { get; set; }
        public double Humidity { get; set; }
        public double Pressure { get; set; }
        public double WindSpeed { get; set; }
        public string Description { get; set; } = string.Empty;
        public string IconCode { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
```

### 8.2: Models/ForecastData.cs
```csharp
using System;

namespace WeatherDashboard.Models
{
    public class ForecastData
    {
        public DateTime Date { get; set; }
        public double TempMax { get; set; }
        public double TempMin { get; set; }
        public string Description { get; set; } = string.Empty;
        public string IconCode { get; set; } = string.Empty;
        public double Humidity { get; set; }
        public double WindSpeed { get; set; }
    }
}
```

---

## Step 9: Add Service Interfaces

### Services/Interfaces/IWeatherService.cs
```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using WeatherDashboard.Models;

namespace WeatherDashboard.Services.Interfaces
{
    public interface IWeatherService
    {
        Task<WeatherData> GetCurrentWeatherAsync(string cityName);
        Task<WeatherData> GetCurrentWeatherAsync(double lat, double lon);
        Task<List<ForecastData>> GetForecastAsync(string cityName);
        Task<List<ForecastData>> GetForecastAsync(double lat, double lon);
        void SetApiKey(string apiKey);
    }
}
```

### Services/Interfaces/IDataService.cs
```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WeatherDashboard.Data.Entities;

namespace WeatherDashboard.Services.Interfaces
{
    public interface IDataService
    {
        // Weather Records
        Task SaveWeatherRecordAsync(WeatherRecord record);
        Task<List<WeatherRecord>> GetWeatherHistoryAsync(int locationId, DateTime from, DateTime to);
        
        // Locations
        Task<List<SavedLocation>> GetAllLocationsAsync();
        Task<SavedLocation?> GetLocationByIdAsync(int id);
        Task<SavedLocation?> GetLocationByNameAsync(string name);
        Task<SavedLocation> AddLocationAsync(SavedLocation location);
        Task DeleteLocationAsync(int id);
        Task<SavedLocation?> GetDefaultLocationAsync();
        
        // Settings
        Task<string?> GetSettingAsync(string key, string? defaultValue = null);
        Task SaveSettingAsync(string key, string value);
    }
}
```

---

## Step 10: Implement Services

### Services/WeatherApiService.cs
```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using WeatherDashboard.Models;
using WeatherDashboard.Services.Interfaces;

namespace WeatherDashboard.Services
{
    public class WeatherApiService : IWeatherService
    {
        private readonly HttpClient _httpClient;
        private string _apiKey = "YOUR_API_KEY_HERE"; // Will be set from settings

        public WeatherApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("https://api.openweathermap.org/data/2.5/");
        }

        public void SetApiKey(string apiKey)
        {
            _apiKey = apiKey;
        }

        public async Task<WeatherData> GetCurrentWeatherAsync(string cityName)
        {
            var url = $"weather?q={Uri.EscapeDataString(cityName)}&appid={_apiKey}&units=metric";
            var json = await _httpClient.GetStringAsync(url);
            var response = JsonSerializer.Deserialize<CurrentWeatherResponse>(json, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (response == null)
                throw new Exception("Failed to deserialize weather data");

            return MapToWeatherData(response);
        }

        public async Task<WeatherData> GetCurrentWeatherAsync(double lat, double lon)
        {
            var url = $"weather?lat={lat}&lon={lon}&appid={_apiKey}&units=metric";
            var json = await _httpClient.GetStringAsync(url);
            var response = JsonSerializer.Deserialize<CurrentWeatherResponse>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (response == null)
                throw new Exception("Failed to deserialize weather data");

            return MapToWeatherData(response);
        }

        public async Task<List<ForecastData>> GetForecastAsync(string cityName)
        {
            var url = $"forecast?q={Uri.EscapeDataString(cityName)}&appid={_apiKey}&units=metric";
            var json = await _httpClient.GetStringAsync(url);
            var response = JsonSerializer.Deserialize<ForecastResponse>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (response == null)
                throw new Exception("Failed to deserialize forecast data");

            return MapToForecastData(response);
        }

        public async Task<List<ForecastData>> GetForecastAsync(double lat, double lon)
        {
            var url = $"forecast?lat={lat}&lon={lon}&appid={_apiKey}&units=metric";
            var json = await _httpClient.GetStringAsync(url);
            var response = JsonSerializer.Deserialize<ForecastResponse>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (response == null)
                throw new Exception("Failed to deserialize forecast data");

            return MapToForecastData(response);
        }

        private WeatherData MapToWeatherData(CurrentWeatherResponse response)
        {
            return new WeatherData
            {
                LocationName = response.Name ?? "Unknown",
                Country = response.Sys?.Country ?? "Unknown",
                Latitude = response.Coord?.Lat ?? 0,
                Longitude = response.Coord?.Lon ?? 0,
                Temperature = response.Main?.Temp ?? 0,
                FeelsLike = response.Main?.Feels_Like ?? 0,
                Humidity = response.Main?.Humidity ?? 0,
                Pressure = response.Main?.Pressure ?? 0,
                WindSpeed = response.Wind?.Speed ?? 0,
                Description = response.Weather?.FirstOrDefault()?.Description ?? "Unknown",
                IconCode = response.Weather?.FirstOrDefault()?.Icon ?? "01d",
                Timestamp = DateTime.UtcNow
            };
        }

        private List<ForecastData> MapToForecastData(ForecastResponse response)
        {
            if (response.List == null)
                return new List<ForecastData>();

            return response.List
                .GroupBy(f => DateTimeOffset.FromUnixTimeSeconds(f.Dt).Date)
                .Select(g => g.OrderBy(f => Math.Abs(DateTimeOffset.FromUnixTimeSeconds(f.Dt).Hour - 12)).First())
                .Take(5)
                .Select(f => new ForecastData
                {
                    Date = DateTimeOffset.FromUnixTimeSeconds(f.Dt).DateTime,
                    TempMax = f.Main?.Temp_Max ?? 0,
                    TempMin = f.Main?.Temp_Min ?? 0,
                    Description = f.Weather?.FirstOrDefault()?.Description ?? "Unknown",
                    IconCode = f.Weather?.FirstOrDefault()?.Icon ?? "01d",
                    Humidity = f.Main?.Humidity ?? 0,
                    WindSpeed = f.Wind?.Speed ?? 0
                })
                .ToList();
        }

        #region API Response Models

        private class CurrentWeatherResponse
        {
            public CoordData? Coord { get; set; }
            public List<WeatherDescription>? Weather { get; set; }
            public MainData? Main { get; set; }
            public WindData? Wind { get; set; }
            public string? Name { get; set; }
            public SysData? Sys { get; set; }
        }

        private class ForecastResponse
        {
            public List<ForecastItem>? List { get; set; }
        }

        private class ForecastItem
        {
            public long Dt { get; set; }
            public MainData? Main { get; set; }
            public List<WeatherDescription>? Weather { get; set; }
            public WindData? Wind { get; set; }
        }

        private class CoordData
        {
            public double Lat { get; set; }
            public double Lon { get; set; }
        }

        private class MainData
        {
            public double Temp { get; set; }
            public double Feels_Like { get; set; }
            public double Humidity { get; set; }
            public double Pressure { get; set; }
            public double Temp_Min { get; set; }
            public double Temp_Max { get; set; }
        }

        private class WeatherDescription
        {
            public string? Main { get; set; }
            public string? Description { get; set; }
            public string? Icon { get; set; }
        }

        private class WindData
        {
            public double Speed { get; set; }
        }

        private class SysData
        {
            public string? Country { get; set; }
        }

        #endregion
    }
}
```

### Services/DataService.cs
```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WeatherDashboard.Data;
using WeatherDashboard.Data.Entities;
using WeatherDashboard.Services.Interfaces;

namespace WeatherDashboard.Services
{
    public class DataService : IDataService
    {
        private readonly WeatherDbContext _context;

        public DataService(WeatherDbContext context)
        {
            _context = context;
        }

        #region Weather Records

        public async Task SaveWeatherRecordAsync(WeatherRecord record)
        {
            _context.WeatherRecords.Add(record);
            await _context.SaveChangesAsync();
        }

        public async Task<List<WeatherRecord>> GetWeatherHistoryAsync(int locationId, DateTime from, DateTime to)
        {
            return await _context.WeatherRecords
                .Where(w => w.LocationId == locationId && w.Timestamp >= from && w.Timestamp <= to)
                .OrderBy(w => w.Timestamp)
                .ToListAsync();
        }

        #endregion

        #region Locations

        public async Task<List<SavedLocation>> GetAllLocationsAsync()
        {
            return await _context.SavedLocations
                .OrderByDescending(l => l.IsFavorite)
                .ThenBy(l => l.Name)
                .ToListAsync();
        }

        public async Task<SavedLocation?> GetLocationByIdAsync(int id)
        {
            return await _context.SavedLocations.FindAsync(id);
        }

        public async Task<SavedLocation?> GetLocationByNameAsync(string name)
        {
            return await _context.SavedLocations
                .FirstOrDefaultAsync(l => l.Name.ToLower() == name.ToLower());
        }

        public async Task<SavedLocation> AddLocationAsync(SavedLocation location)
        {
            var existing = await GetLocationByNameAsync(location.Name);
            if (existing != null)
                return existing;

            location.CreatedDate = DateTime.Now;
            _context.SavedLocations.Add(location);
            await _context.SaveChangesAsync();

            return location;
        }

        public async Task DeleteLocationAsync(int id)
        {
            var location = await _context.SavedLocations.FindAsync(id);
            if (location != null)
            {
                _context.SavedLocations.Remove(location);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<SavedLocation?> GetDefaultLocationAsync()
        {
            var defaultLocationIdStr = await GetSettingAsync("DefaultLocationId");
            
            if (int.TryParse(defaultLocationIdStr, out int defaultLocationId))
            {
                return await GetLocationByIdAsync(defaultLocationId);
            }

            return await _context.SavedLocations
                .OrderByDescending(l => l.IsFavorite)
                .ThenBy(l => l.CreatedDate)
                .FirstOrDefaultAsync();
        }

        #endregion

        #region Settings

        public async Task<string?> GetSettingAsync(string key, string? defaultValue = null)
        {
            var setting = await _context.UserSettings
                .FirstOrDefaultAsync(s => s.Key == key);

            return setting?.Value ?? defaultValue;
        }

        public async Task SaveSettingAsync(string key, string value)
        {
            var setting = await _context.UserSettings
                .FirstOrDefaultAsync(s => s.Key == key);

            if (setting != null)
            {
                setting.Value = value;
                setting.LastModified = DateTime.Now;
                _context.UserSettings.Update(setting);
            }
            else
            {
                setting = new UserSetting
                {
                    Key = key,
                    Value = value,
                    LastModified = DateTime.Now
                };
                _context.UserSettings.Add(setting);
            }

            await _context.SaveChangesAsync();
        }

        #endregion
    }
}
```

---

## Step 11: Build Again (Checkpoint 2)

```powershell
dotnet build
```

**Expected Result:** Build succeeded with 0 errors.

---

## Step 12: Update App.xaml.cs

Replace the entire content of `App.xaml.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;
using WeatherDashboard.Data;
using WeatherDashboard.Services;
using WeatherDashboard.Services.Interfaces;

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
```

---

## Step 13: Final Build (Phase 1 Complete!)

```powershell
dotnet build
```

**Expected Result:** Build succeeded with 0 errors, 0 warnings.

---

## Phase 1 Completion Checklist

- [ ] All NuGet packages installed
- [ ] All folders created
- [ ] All 13 code files added
- [ ] Database migration created and applied
- [ ] Project builds with 0 errors
- [ ] Database file exists at `%LOCALAPPDATA%\WeatherDashboard\weather.db`

---

## What You Have Now

✅ Complete database layer with EF Core  
✅ Service layer with API integration  
✅ Dependency injection configured  
✅ Database migrations working  
✅ Zero build errors

---

## Next Steps

**Phase 2** will add:
- ViewModels with MVVM pattern
- Basic UI with MainWindow
- Dashboard view with weather display

**Do not proceed to Phase 2 until this phase builds successfully with 0 errors!**

---

## Troubleshooting

### Build Error: "Nullable reference types"
Add this to your `.csproj`:
```xml
<Nullable>enable</Nullable>
```

### Migration Error: "Unable to create DbContext"
Make sure `WeatherDbContextFactory.cs` exists in the `Data` folder.

### Package Restore Error
```powershell
dotnet nuget locals all --clear
dotnet restore
```

---

## Files Created in Phase 1

```
Data/
├── Entities/
│   ├── WeatherRecord.cs
│   ├── SavedLocation.cs
│   └── UserSetting.cs
├── WeatherDbContext.cs
└── WeatherDbContextFactory.cs

Models/
├── WeatherData.cs
└── ForecastData.cs

Services/
├── Interfaces/
│   ├── IWeatherService.cs
│   └── IDataService.cs
├── WeatherApiService.cs
└── DataService.cs

App.xaml.cs (modified)
```

**Total Files:** 13  
**Total Lines of Code:** ~850

---

**Ready to proceed to Phase 2?** Make sure this phase is 100% complete first!
