# Technical Architecture Documentation

## Table of Contents
1. [System Overview](#system-overview)
2. [Architecture Patterns](#architecture-patterns)
3. [Data Flow](#data-flow)
4. [Database Schema](#database-schema)
5. [API Integration](#api-integration)
6. [Key Components](#key-components)
7. [Dependency Injection](#dependency-injection)
8. [Error Handling](#error-handling)
9. [Performance Considerations](#performance-considerations)
10. [Security](#security)

---

## System Overview

Weather Dashboard is a desktop application built using WPF and .NET 8, following MVVM architectural pattern with Entity Framework Core for data persistence and RESTful API integration for weather data.

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    Presentation Layer                    │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐              │
│  │Dashboard │  │ History  │  │ Settings │  (Views)     │
│  │  View    │  │   View   │  │   View   │              │
│  └────┬─────┘  └────┬─────┘  └────┬─────┘              │
│       │             │              │                     │
│  ┌────▼─────┐  ┌───▼──────┐  ┌───▼──────┐             │
│  │Dashboard │  │ History  │  │ Settings │  (ViewModels)│
│  │ViewModel │  │ViewModel │  │ViewModel │              │
│  └────┬─────┘  └────┬─────┘  └────┬─────┘              │
└───────┼─────────────┼─────────────┼────────────────────┘
        │             │              │
┌───────▼─────────────▼──────────────▼────────────────────┐
│                    Business Layer                        │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  │
│  │   Weather    │  │     Data     │  │    Report    │  │
│  │   Service    │  │   Service    │  │   Service    │  │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘  │
└─────────┼──────────────────┼──────────────────┼─────────┘
          │                  │                  │
┌─────────▼──────────┐  ┌───▼──────────────────▼─────────┐
│  OpenWeatherMap    │  │      Data Access Layer         │
│       API          │  │   ┌────────────────────────┐   │
│                    │  │   │  WeatherDbContext      │   │
│  - Current Weather │  │   │  (Entity Framework)    │   │
│  - 5-day Forecast  │  │   └───────────┬────────────┘   │
└────────────────────┘  └───────────────┼────────────────┘
                                        │
                        ┌───────────────▼────────────────┐
                        │      SQLite Database           │
                        │  - WeatherRecords              │
                        │  - SavedLocations              │
                        │  - UserSettings                │
                        └────────────────────────────────┘
```

---

## Architecture Patterns

### 1. MVVM (Model-View-ViewModel)

**Views (XAML)**
- Pure UI markup with no business logic
- Data binding to ViewModel properties
- Command binding for user interactions

**ViewModels**
- Presentation logic and state management
- ObservableProperty for data binding
- RelayCommand for user actions
- No direct reference to Views

**Models**
- Domain entities and data transfer objects
- No UI or data access logic
- Plain C# classes

**Benefits:**
- Separation of concerns
- Testability (ViewModels can be unit tested)
- Designer-developer workflow
- Code reusability

### 2. Repository Pattern

DataService acts as a repository, abstracting data access:

```csharp
public interface IDataService
{
    Task<List<SavedLocation>> GetAllLocationsAsync();
    Task<SavedLocation> AddLocationAsync(SavedLocation location);
    Task<List<WeatherRecord>> GetWeatherHistoryAsync(int locationId, DateTime from, DateTime to);
}
```

**Benefits:**
- Testability (can mock data access)
- Flexibility (can swap data sources)
- Centralized data access logic

### 3. Dependency Injection

Services registered in `App.xaml.cs` and injected via constructors:

```csharp
services.AddSingleton<IWeatherService, WeatherApiService>();
services.AddScoped<IDataService, DataService>();
services.AddTransient<DashboardViewModel>();
```

**Service Lifetimes:**
- **Singleton:** One instance for application lifetime (WeatherService)
- **Scoped:** One instance per request/scope (DataService)
- **Transient:** New instance every time (ViewModels)

---

## Data Flow

### Viewing Current Weather

```
User Action: Select Location
        ↓
DashboardViewModel.OnSelectedLocationChanged()
        ↓
LoadWeatherCommand.Execute()
        ↓
WeatherApiService.GetCurrentWeatherAsync()
        ↓
[HTTP GET] → OpenWeatherMap API
        ↓
JSON Response → Deserialized to CurrentWeatherResponse
        ↓
Map to WeatherData (domain model)
        ↓
DataService.SaveWeatherRecordAsync()
        ↓
EF Core → SQLite Database
        ↓
Update CurrentWeather Property
        ↓
UI Auto-Updates via Data Binding
```

### Viewing Historical Data

```
User Action: Navigate to History Tab
        ↓
HistoryViewModel.InitializeAsync()
        ↓
DataService.GetWeatherHistoryAsync()
        ↓
EF Core LINQ Query → SQLite
        ↓
List<WeatherRecord> returned
        ↓
UpdateTemperatureChart() / UpdateHumidityChart()
        ↓
Create ScottPlot.Plot objects
        ↓
Set TemperaturePlot / HumidityPlot Properties
        ↓
PropertyChanged Event
        ↓
HistoryView.Vm_PropertyChanged()
        ↓
WpfPlot.Reset(plot)
        ↓
Charts Rendered
```

---

## Database Schema

### Entity Relationship Diagram

```
┌─────────────────────┐
│   SavedLocation     │
├─────────────────────┤
│ Id (PK)             │
│ Name                │
│ Latitude            │
│ Longitude           │
│ Country             │
│ IsFavorite          │
│ CreatedDate         │
└──────────┬──────────┘
           │ 1
           │
           │ *
┌──────────▼──────────┐
│   WeatherRecord     │
├─────────────────────┤
│ Id (PK)             │
│ LocationId (FK)     │
│ Timestamp           │
│ Temperature         │
│ FeelsLike           │
│ Humidity            │
│ Pressure            │
│ WindSpeed           │
│ Description         │
│ IconCode            │
└─────────────────────┘

┌─────────────────────┐
│   UserSetting       │
├─────────────────────┤
│ Id (PK)             │
│ Key (UNIQUE)        │
│ Value               │
│ LastModified        │
└─────────────────────┘
```

### Table Details

**SavedLocation**
- Stores user's saved weather locations
- Indexed on `Name` for fast lookups
- One-to-many relationship with WeatherRecord

**WeatherRecord**
- Historical weather data points
- Composite index on `(LocationId, Timestamp)` for efficient queries
- Cascade delete when location is removed

**UserSetting**
- Key-value pairs for application configuration
- Common settings: ApiKey, TemperatureUnit, DefaultLocationId
- Unique constraint on Key

### Sample EF Core Configuration

```csharp
modelBuilder.Entity<WeatherRecord>(entity =>
{
    entity.HasKey(e => e.Id);
    entity.HasIndex(e => new { e.LocationId, e.Timestamp });
    
    entity.HasOne(e => e.Location)
        .WithMany(l => l.WeatherRecords)
        .HasForeignKey(e => e.LocationId)
        .OnDelete(DeleteBehavior.Cascade);
});
```

---

## API Integration

### OpenWeatherMap REST API

**Base URL:** `https://api.openweathermap.org/data/2.5/`

**Endpoints Used:**

1. **Current Weather**
   ```
   GET /weather?lat={lat}&lon={lon}&appid={key}&units=metric
   ```
   Returns current weather conditions for coordinates

2. **5-Day Forecast**
   ```
   GET /forecast?lat={lat}&lon={lon}&appid={key}&units=metric
   ```
   Returns 3-hour forecast data for 5 days

### API Response Handling

```csharp
public async Task<WeatherData> GetCurrentWeatherAsync(double lat, double lon)
{
    var url = $"weather?lat={lat}&lon={lon}&appid={_apiKey}&units=metric";
    
    var response = await _httpClient.GetAsync(url);
    response.EnsureSuccessStatusCode();
    
    var json = await response.Content.ReadAsStringAsync();
    var apiResponse = JsonSerializer.Deserialize<CurrentWeatherResponse>(json);
    
    return MapToWeatherData(apiResponse);
}
```

### Error Scenarios Handled

- **401 Unauthorized:** Invalid API key
- **404 Not Found:** Location doesn't exist
- **429 Too Many Requests:** Rate limit exceeded
- **Network errors:** Timeout, connection failure
- **JSON parsing errors:** Malformed response

---

## Key Components

### ViewModelBase

Base class for all ViewModels providing common functionality:

```csharp
public abstract class ViewModelBase : ObservableObject
{
    protected async Task ExecuteAsync(Func<Task> operation, string errorMessage)
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
```

**Features:**
- IsBusy flag for loading states
- Centralized error handling
- Success/error message management
- INotifyPropertyChanged implementation (via ObservableObject)

### DashboardViewModel

Main ViewModel for weather display:

**Key Properties:**
- `CurrentWeather` - Current conditions
- `Forecast` - 5-day forecast
- `Locations` - Saved locations list
- `SelectedLocation` - Currently selected location

**Key Commands:**
- `SearchLocationCommand` - Search and add new location
- `RefreshCommand` - Reload current weather
- `SetDefaultLocationCommand` - Set preferred location
- `ToggleTemperatureUnitCommand` - Switch °C/°F

**Auto-loading Logic:**
```csharp
partial void OnSelectedLocationChanged(SavedLocation? value)
{
    if (value != null)
    {
        _ = LoadWeatherAsync(); // Async fire-and-forget
    }
}
```

### HistoryViewModel

ViewModel for historical data visualization:

**Key Properties:**
- `WeatherHistory` - List of historical records
- `TemperaturePlot` - ScottPlot chart for temperature
- `HumidityPlot` - ScottPlot chart for humidity
- `StartDate` / `EndDate` - Date range filters

**Chart Generation:**
```csharp
private void UpdateTemperatureChart()
{
    var plot = new Plot();
    var dates = WeatherHistory.Select(r => r.Timestamp.ToOADate()).ToArray();
    var temps = WeatherHistory.Select(r => r.Temperature).ToArray();
    
    var scatter = plot.Add.Scatter(dates, temps);
    scatter.Color = Colors.Red;
    scatter.LineWidth = 2;
    
    plot.Axes.DateTimeTicksBottom();
    plot.Title("Temperature History");
    
    TemperaturePlot = plot; // Triggers PropertyChanged
}
```

---

## Dependency Injection

### Service Registration

```csharp
private void ConfigureServices(IServiceCollection services)
{
    // DbContext with scoped lifetime
    services.AddDbContext<WeatherDbContext>(options =>
        options.UseSqlite($"Data Source={dbPath}"));
    
    // Singleton services (one instance)
    services.AddSingleton<IWeatherService, WeatherApiService>();
    
    // Scoped services (per request)
    services.AddScoped<IDataService, DataService>();
    services.AddScoped<IReportService, ReportService>();
    
    // HttpClient factory
    services.AddHttpClient<IWeatherService, WeatherApiService>(client =>
    {
        client.BaseAddress = new Uri("https://api.openweathermap.org/data/2.5/");
        client.Timeout = TimeSpan.FromSeconds(30);
    });
    
    // Transient ViewModels (new instance each time)
    services.AddTransient<DashboardViewModel>();
    services.AddTransient<HistoryViewModel>();
}
```

### Resolution

```csharp
// In MainWindow
var viewModel = _serviceProvider.GetRequiredService<DashboardViewModel>();
```

Constructor injection automatically resolves dependencies:
```csharp
public DashboardViewModel(
    IWeatherService weatherService,  // Resolved automatically
    IDataService dataService)         // Resolved automatically
{
    _weatherService = weatherService;
    _dataService = dataService;
}
```

---

## Error Handling

### Layered Approach

**1. Service Layer**
- Catch HTTP exceptions
- Throw custom exceptions with meaningful messages

**2. ViewModel Layer**
- Use `ExecuteAsync` wrapper
- Set `ErrorMessage` property
- Log errors for debugging

**3. View Layer**
- Display error messages to user
- Provide retry options
- Disable actions during errors

### Example

```csharp
[RelayCommand]
private async Task SearchLocationAsync()
{
    await ExecuteAsync(async () =>
    {
        var weather = await _weatherService.GetCurrentWeatherAsync(SearchText);
        // Success path
    }, 
    "Location not found. Please check the spelling and try again.");
}
```

User sees: "Location not found. Please check the spelling and try again: [specific error]"

---

## Performance Considerations

### Database Optimization

**Indexes:**
```csharp
entity.HasIndex(e => new { e.LocationId, e.Timestamp });
```
Speeds up historical queries by location and date range.

**Async Operations:**
All database operations use async/await to prevent UI blocking.

**Eager/Lazy Loading:**
Explicit loading strategy - only load what's needed.

### API Optimization

**Rate Limiting:**
- Free tier: 1,000 calls/day
- App stores results to minimize API calls
- Historical data comes from database

**Caching Strategy:**
- Weather data saved to database
- Only fetch new data when explicitly requested
- Could implement time-based caching (future enhancement)

### UI Performance

**Data Binding:**
- One-way binding where possible
- UpdateSourceTrigger on PropertyChanged

**Chart Rendering:**
- ScottPlot handles large datasets efficiently
- Interaction disabled on empty charts (prevents crashes)
- Charts updated only when data changes

**Lazy Loading:**
- Views initialized only when navigated to
- Data loaded on-demand

---

## Security

### API Key Management

**Storage:**
- Stored in SQLite database (UserSettings table)
- Not hardcoded in source
- Added to `.gitignore` to prevent accidental commits

**Best Practices:**
- User enters key on first run
- Could be encrypted (future enhancement)
- Never logged or displayed

### SQL Injection Prevention

**Entity Framework Protection:**
- Parameterized queries automatically
- No raw SQL used
- LINQ provides type safety

### Data Validation

**Input Validation:**
- Location search validated before API call
- Date ranges validated (start ≤ end)
- Numeric values validated in UI

---

## Code Quality

### SOLID Principles

**S - Single Responsibility**
- Each class has one clear purpose
- ViewModels handle presentation logic only
- Services handle specific domains (API, Data, Reports)

**O - Open/Closed**
- ViewModelBase extensible via inheritance
- Services implement interfaces

**L - Liskov Substitution**
- All ViewModels can be treated as ViewModelBase
- Service implementations interchangeable via interfaces

**I - Interface Segregation**
- Focused interfaces (IWeatherService, IDataService, IReportService)
- No "god" interfaces

**D - Dependency Inversion**
- Depend on abstractions (interfaces) not concrete types
- Dependency injection enforces this

### Naming Conventions

- **Classes:** PascalCase (DashboardViewModel)
- **Methods:** PascalCase (LoadWeatherAsync)
- **Properties:** PascalCase (CurrentWeather)
- **Private fields:** _camelCase (_weatherService)
- **Async methods:** Suffix with "Async"

### Documentation

- XML comments on public APIs
- Inline comments for complex logic
- README for project overview
- This document for architecture

---

## Testing Strategy

### Unit Tests (Planned)

**ViewModels:**
- Test commands execute correctly
- Test property changes notify
- Mock services to isolate logic

**Services:**
- Test API mapping logic
- Test database operations
- Test error handling

**Example:**
```csharp
[Fact]
public async Task SearchLocation_ValidCity_LoadsWeather()
{
    // Arrange
    var mockWeatherService = new Mock<IWeatherService>();
    var viewModel = new DashboardViewModel(mockWeatherService.Object, ...);
    
    // Act
    viewModel.SearchText = "London";
    await viewModel.SearchLocationCommand.ExecuteAsync(null);
    
    // Assert
    Assert.NotNull(viewModel.CurrentWeather);
    Assert.Equal("London", viewModel.CurrentWeather.LocationName);
}
```

### Integration Tests (Planned)

- Test database operations with test database
- Test API calls with test API key
- Test end-to-end workflows

---

## Build & Deployment

### Build Process

```bash
dotnet restore              # Restore NuGet packages
dotnet build               # Compile
dotnet ef database update  # Apply migrations
dotnet run                 # Run application
```

### Publish

```bash
dotnet publish -c Release -r win-x64 --self-contained
```

Creates standalone executable with .NET runtime included.

### Database Migrations

```bash
dotnet ef migrations add MigrationName
dotnet ef database update
```

---

## Maintenance

### Adding New Features

1. Create/update service interface
2. Implement service
3. Register in DI container
4. Create/update ViewModel
5. Create/update View
6. Test

### Database Changes

1. Update entity classes
2. Create migration: `dotnet ef migrations add DescriptiveName`
3. Review generated migration
4. Apply: `dotnet ef database update`

### Troubleshooting

**Database locked:**
- Ensure all DbContext instances are disposed
- Check for long-running queries

**API errors:**
- Verify API key is valid
- Check internet connection
- Review rate limits

**UI not updating:**
- Ensure property implements INotifyPropertyChanged
- Verify binding path is correct
- Check DataContext is set

---

*This technical documentation provides a comprehensive overview of the Weather Dashboard architecture for developers and technical stakeholders.*
