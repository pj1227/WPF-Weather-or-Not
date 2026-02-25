# State Management Pattern

This document describes the singleton state management architecture used in Weather Dashboard, covering both shared location state and the global temperature unit toggle.

---

## The Problem

In a multi-view WPF application using MVVM and dependency injection, two pieces of state must be consistent across all views:

1. **Selected Location** — Dashboard and History must always show data for the same city
2. **Temperature Unit (°C / °F)** — Every temperature value in every view must reflect the same preference, and the toggle must be operable from anywhere without navigating to a specific view

---

## The Solution: Singleton State Service + INotifyPropertyChanged

A singleton `ApplicationStateService` acts as the single source of truth. It propagates changes through the standard `INotifyPropertyChanged` mechanism — no custom events required.

```
┌───────────────────────────────────────────────────────────┐
│                    MainWindow (Shell)                     │
│  ┌────────────────────────────────────────────────────┐   │
│  │  Navigation Sidebar (200px)                        │   │
│  │  • 📊 Dashboard RadioButton → NavButton_Checked    │   │
│  │  • 📈 History   RadioButton → NavButton_Checked    │   │
│  │  • °C/°F ToggleButton → UseCelsius (TwoWay)        │   │
│  └────────────────────────────────────────────────────┘   │
│  ┌────────────────────────────────────────────────────┐   │
│  │  ContentControl x:Name="ContentArea"               │   │
│  │  Content set in NavButton_Checked code-behind      │   │
│  └────────────────────────────────────────────────────┘   │
└───────────────────────────────────────────────────────────┘
         ↕ DataBinding              ↕ DataBinding
┌─────────────────────┐    ┌────────────────────────────────┐
│   ShellViewModel    │    │  DashboardViewModel            │
│   (Singleton)       │    │  HistoryViewModel              │
│   • UseCelsius ─────┤    │  (both Singleton)              │
│     delegates to    │    │  • UseCelsius (delegated)      │
│     StateService    │    │  • OnTemperatureUnitChanged()  │
└──────────┬──────────┘    └────────────────┬───────────────┘
           │                                │
           │        PropertyChanged         │
           └────────────────┬───────────────┘
                            ▼
         ┌──────────────────────────────────────┐
         │       ApplicationStateService        │  ← Singleton
         │       (ObservableObject)             │
         │                                      │
         │   bool UseCelsius                    │
         │   SavedLocation? SelectedLocation    │
         │                                      │
         │   PropertyChanged fires via          │
         │   CommunityToolkit SetProperty()     │
         └──────────────────────────────────────┘
```

---

## Components

### IApplicationStateService

```csharp
public interface IApplicationStateService : INotifyPropertyChanged
{
    SavedLocation? SelectedLocation { get; set; }
    bool UseCelsius { get; set; }
}
```

Extends `INotifyPropertyChanged` — that's the complete signalling contract. No custom events. Subscribers use the standard `PropertyChanged` event with a property name check.

### ApplicationStateService

```csharp
public class ApplicationStateService : ObservableObject, IApplicationStateService
{
    private SavedLocation? _selectedLocation;
    private bool _useCelsius = true;

    public SavedLocation? SelectedLocation
    {
        get => _selectedLocation;
        set => SetProperty(ref _selectedLocation, value);
    }

    public bool UseCelsius
    {
        get => _useCelsius;
        set => SetProperty(ref _useCelsius, value);
    }
}
```

Deliberately minimal. `SetProperty` handles change detection and fires `PropertyChanged`. No custom events, no additional methods.

### ViewModelBase

All feature ViewModels inherit `ViewModelBase`. It subscribes to `StateService.PropertyChanged` and routes changes through two virtual methods that derived classes override:

```csharp
protected ViewModelBase(IDataService dataService, IApplicationStateService stateService)
{
    DataService  = dataService;
    StateService = stateService;
    StateService.PropertyChanged += StateService_PropertyChanged;
}

private void StateService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
{
    if (e.PropertyName == nameof(StateService.SelectedLocation))
    {
        OnPropertyChanged(nameof(SelectedLocation));
        OnSelectedLocationChanged();        // virtual hook
    }
    if (e.PropertyName == nameof(StateService.UseCelsius))
    {
        OnPropertyChanged(nameof(UseCelsius));
        OnTemperatureUnitChanged();         // virtual hook
    }
}

protected virtual void OnSelectedLocationChanged() { }
protected virtual void OnTemperatureUnitChanged()  { }
```

`SelectedLocation` and `UseCelsius` are delegation properties with no backing fields — they always read the live singleton value.

### TemperatureFormatter (Helpers/TemperatureFormatter.cs)

A static utility class centralizing all conversion math so it can't drift out of sync between ViewModels:

```csharp
public static class TemperatureFormatter
{
    public static double ToFahrenheit(double celsius) => (celsius * 9.0 / 5.0) + 32.0;
    public static double ToCelsius(double fahrenheit)  => (fahrenheit - 32.0) * 5.0 / 9.0;

    public static string Format(double celsius, bool useCelsius)
    {
        if (useCelsius) return $"{celsius:F1}°C";
        return $"{ToFahrenheit(celsius):F1}°F";
    }
}
```

---

## The Temperature Unit Toggle

### UI — MainWindow.xaml

The toggle is a custom `ToggleButton` in the sidebar. `MainWindow.DataContext` is `ShellViewModel`. Navigation between views uses a `NavButton_Checked` code-behind handler that sets `ContentArea.Content` directly.

### ViewModel — ShellViewModel

`ShellViewModel` does not inherit `ViewModelBase` — it only needs to manage the shell binding. It delegates `UseCelsius` to `ApplicationStateService` and subscribes to `PropertyChanged` so the toggle reflects values loaded at startup:

```csharp
public class ShellViewModel : ObservableObject
{
    private readonly IApplicationStateService _stateService;

    public ShellViewModel(IApplicationStateService stateService)
    {
        _stateService = stateService;

        // Required: when App.OnStartup loads the saved unit from the database
        // and sets UseCelsius on the service, the ToggleButton must be notified.
        _stateService.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(IApplicationStateService.UseCelsius))
                OnPropertyChanged(nameof(UseCelsius));
        };
    }

    public bool UseCelsius
    {
        get => _stateService.UseCelsius;
        set => _stateService.UseCelsius = value;
    }
}
```

---

## Complete Event Flow

### User clicks the toggle

```
ToggleButton.IsChecked changes  (TwoWay XAML binding)
    │
    ▼
ShellViewModel.UseCelsius setter
    │  writes to _stateService.UseCelsius
    ▼
ApplicationStateService.UseCelsius setter
    │  SetProperty → value stored + PropertyChanged("UseCelsius") fired
    │
    ├──▶ ShellViewModel subscription
    │       OnPropertyChanged("UseCelsius")  ← keeps toggle in sync for startup
    │
    ├──▶ ViewModelBase.StateService_PropertyChanged (DashboardViewModel)
    │       OnPropertyChanged("UseCelsius")
    │       → OnTemperatureUnitChanged() [override in DashboardViewModel]
    │           OnPropertyChanged("FormattedTemperature")
    │           OnPropertyChanged("FormattedFeelsLike")
    │           foreach ForecastData item:
    │             item.TempMaxDisplay = converted value  (raises PropertyChanged on item)
    │             item.TempMinDisplay = converted value  (raises PropertyChanged on item)
    │             → ItemsControl cards update via item's own PropertyChanged
    │
    └──▶ ViewModelBase.StateService_PropertyChanged (HistoryViewModel)
            OnPropertyChanged("UseCelsius")
            → OnTemperatureUnitChanged() [override in HistoryViewModel]
                UpdateTemperatureChart()   ← new Plot with converted Y values + axis label
                OnPropertyChanged("AverageTemperature")  ← ConvertTemp() returns °F value
                OnPropertyChanged("MaxTemperature")
                OnPropertyChanged("MinTemperature")
                ← HistoryView.xaml.cs picks up TemperaturePlot PropertyChanged,
                   calls WpfPlot.Reset() + Refresh()
```

### Startup: loading saved preference

```
App.OnStartup
    │
    ▼
DataService.GetSettingAsync("TemperatureUnit")
    │  returns "Celsius" or "Fahrenheit"
    ▼
ApplicationStateService.UseCelsius = (unit == "Celsius")
    │  PropertyChanged("UseCelsius") fires to all subscribers
    │
    ├──▶ ShellViewModel → OnPropertyChanged("UseCelsius") → toggle reflects DB value
    ├──▶ DashboardViewModel → OnTemperatureUnitChanged() → formatted props refresh
    └──▶ HistoryViewModel  → OnTemperatureUnitChanged() → charts + stats refresh
    │
    ▼
MainWindow.Show()
    │
    ▼
UI renders with correct unit — no stale display on first paint
```

---

## What Each ViewModel Does

### DashboardViewModel

Computed strings call `TemperatureFormatter.Format` at read time:

```csharp
public string FormattedTemperature =>
    CurrentWeather == null
        ? "--°"
        : TemperatureFormatter.Format(CurrentWeather.Temperature, StateService.UseCelsius);
```

Forecast items are `ForecastData : ObservableObject` with `[ObservableProperty]` display doubles. Stamped on load and re-stamped on unit toggle. Because `ForecastData` inherits `ObservableObject`, setting `TempMaxDisplay` raises `PropertyChanged` on the item directly — the `ItemsControl` cards update without replacing the collection:

```csharp
// Override in DashboardViewModel
protected override void OnTemperatureUnitChanged()
{
    OnPropertyChanged(nameof(FormattedTemperature));
    OnPropertyChanged(nameof(FormattedFeelsLike));

    foreach (var item in Forecast)
    {
        item.TempMaxDisplay = StateService.UseCelsius
            ? item.TempMax : TemperatureFormatter.ToFahrenheit(item.TempMax);
        item.TempMinDisplay = StateService.UseCelsius
            ? item.TempMin : TemperatureFormatter.ToFahrenheit(item.TempMin);
    }
}
```

### HistoryViewModel

Statistics convert at read time via `ConvertTemp`:

```csharp
private double ConvertTemp(double celsius) =>
    StateService.UseCelsius ? celsius : TemperatureFormatter.ToFahrenheit(celsius);

public double AverageTemperature => WeatherHistory.Any()
    ? ConvertTemp(WeatherHistory.Average(r => r.Temperature)) : 0;
```

`OnTemperatureUnitChanged` rebuilds charts and re-raises stat properties. `HistoryView.xaml.cs` subscribes to `vm.PropertyChanged` and calls `WpfPlot.Reset()` + `Refresh()` when `TemperaturePlot` changes.

---

## DI Registration

```csharp
services.AddSingleton<IApplicationStateService, ApplicationStateService>();

// All ViewModels Singleton — StateService_PropertyChanged is subscribed exactly
// once per ViewModel in the constructor. Transient would allow duplicate
// subscriptions if the container resolved them more than once.
services.AddSingleton<ShellViewModel>();
services.AddSingleton<DashboardViewModel>();
services.AddSingleton<HistoryViewModel>();

services.AddScoped<IDataService, DataService>();
services.AddScoped<IReportService, ReportService>();  // receives IApplicationStateService via constructor
```

`ReportService` receives `IApplicationStateService` in its constructor so PDF and Excel reports respect the active temperature unit. Because `ReportService` is `AddScoped` and `ApplicationStateService` is `AddSingleton`, the DI container injects the singleton into the scoped service automatically — no special configuration required. The interface method signatures (`GeneratePdfReportAsync`, `GenerateExcelReportAsync`) are unchanged, so `HistoryViewModel` callers need no updates.

---

## Design Alternatives Considered

| Approach | Why Rejected |
|---|---|
| Custom `TemperatureUnitChanged` event on service | Redundant — `INotifyPropertyChanged` is already the standard mechanism; two signals for the same change would need to be kept in sync |
| Static class with static properties | Not testable; tight coupling; no DI |
| `WeakReferenceMessenger` | Weaker typing; fire-and-forget semantics; adds framework dependency with no benefit here |
| Each ViewModel holds its own `UseCelsius` bool | State diverges between views; no global toggle |
| Shared state directly in `ViewModelBase` | Instances are separate objects — state would not be shared |

---

## Testability

`IApplicationStateService` extends `INotifyPropertyChanged`, so Moq can raise `PropertyChanged` on a mock to simulate the service firing a change — without needing the concrete implementation:

```csharp
var mockState = new Mock<IApplicationStateService>();
mockState.SetupProperty(s => s.UseCelsius, true);

var vm = new DashboardViewModel(mockData.Object, mockState.Object, mockWeather.Object);
vm.CurrentWeather = new WeatherData { Temperature = 0.0, FeelsLike = -5.0 };

// Simulate ApplicationStateService.SetProperty firing PropertyChanged("UseCelsius")
mockState.Object.UseCelsius = false;
mockState.Raise(
    s => s.PropertyChanged += null,
    new PropertyChangedEventArgs(nameof(IApplicationStateService.UseCelsius)));

Assert.Equal("32.0°F", vm.FormattedTemperature);
```

See [TESTING.md](TESTING.md) for the full test suite structure.
