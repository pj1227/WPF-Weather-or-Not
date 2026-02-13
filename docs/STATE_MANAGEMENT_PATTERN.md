# State Management Pattern - Weather Dashboard
## Singleton Service with Property Delegation

**Pattern:** Shared State Service  
**Implementation:** ApplicationStateService (Singleton) + ViewModelBase (Delegation)  
**Result:** Automatic synchronization across ViewModels

---

## 📋 The Problem

In multi-view WPF applications, ViewModels often need to share state:

```
❌ Problem: Duplicate State

DashboardViewModel               HistoryViewModel
├── SelectedLocation: "Billings"  ├── SelectedLocation: "Missoula"
└── UseCelsius: true             └── UseCelsius: false

User selects location in Dashboard → History still shows old location
User toggles temperature unit → Other views don't update
```

**Issues:**
- State gets out of sync
- Poor user experience (must reselect in each view)
- Code duplication
- Manual synchronization logic needed

---

## ✅ The Solution

**Singleton State Service with Property Delegation:**

```
ApplicationStateService (Singleton - ONE instance)
        ↑                           ↑
        |                           |
DashboardViewModel          HistoryViewModel
(both reference same service instance)
```

### Architecture

```
┌─────────────────────────────────────────────┐
│           XAML Views                        │
│  <ComboBox SelectedItem="{Binding           │
│             SelectedLocation}" />           │
└────────────┬────────────────────────────────┘
             ↓ Binding
┌────────────────────────────────────────────┐
│      ViewModels (Transient)                │
│                                            │
│  public SavedLocation? SelectedLocation    │
│  {                                         │
│      get => StateService.SelectedLocation; │ ← Delegates to service
│      set => StateService.SelectedLocation  │
│             = value;                       │
│  }                                         │
└────────────┬───────────────────────────────┘
             ↓ Delegates
┌────────────────────────────────────────────┐
│  ApplicationStateService (Singleton)       │
│                                            │
│  private SavedLocation? _selectedLocation; │
│                                            │
│  public SavedLocation? SelectedLocation    │
│  {                                         │
│      set {                                 │
│          _selectedLocation = value;        │
│          SelectedLocationChanged           │
│              ?.Invoke(this, value);        │ ← Fires event
│      }                                     │
│  }                                         │
│                                            │
│  public event EventHandler<SavedLocation>  │
│      SelectedLocationChanged;              │
└────────────────────────────────────────────┘
```

---

## 💻 Implementation

### 1. ApplicationStateService.cs

```csharp
using System;
using WeatherDashboard.Data.Entities;
using WeatherDashboard.Services.Interfaces;

namespace WeatherDashboard.Services
{
    public class ApplicationStateService : IApplicationStateService
    {
        // Backing fields
        private SavedLocation? _selectedLocation;
        private bool _useCelsius = true;

        // Public state properties
        public SavedLocation? SelectedLocation
        {
            get => _selectedLocation;
            set
            {
                if (_selectedLocation != value)
                {
                    _selectedLocation = value;
                    SelectedLocationChanged?.Invoke(this, value);
                }
            }
        }

        public bool UseCelsius
        {
            get => _useCelsius;
            set
            {
                if (_useCelsius != value)
                {
                    _useCelsius = value;
                    TemperatureUnitChanged?.Invoke(this, value);
                }
            }
        }

        // Event notifications
        public event EventHandler<SavedLocation?>? SelectedLocationChanged;
        public event EventHandler<bool>? TemperatureUnitChanged;
    }
}
```

**Key Points:**
- Simple property implementation
- Fires custom events (not INotifyPropertyChanged)
- No persistence logic (handled elsewhere)
- Lightweight state holder only

---

### 2. IApplicationStateService.cs

```csharp
using System;
using WeatherDashboard.Data.Entities;

namespace WeatherDashboard.Services.Interfaces
{
    public interface IApplicationStateService
    {
        SavedLocation? SelectedLocation { get; set; }
        bool UseCelsius { get; set; }

        event EventHandler<SavedLocation?>? SelectedLocationChanged;
        event EventHandler<bool>? TemperatureUnitChanged;
    }
}
```

---

### 3. ViewModelBase.cs (Delegation Layer)

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using WeatherDashboard.Data.Entities;
using WeatherDashboard.Services.Interfaces;

namespace WeatherDashboard.ViewModels
{
    public abstract class ViewModelBase : ObservableObject
    {
        protected IDataService DataService { get; }
        protected IApplicationStateService StateService { get; }

        // Property wrappers - delegate to service
        public SavedLocation? SelectedLocation
        {
            get => StateService.SelectedLocation;
            set
            {
                if (StateService.SelectedLocation != value)
                {
                    StateService.SelectedLocation = value;
                    OnPropertyChanged();  // Notify THIS ViewModel's view
                }
            }
        }

        public bool UseCelsius
        {
            get => StateService.UseCelsius;
            set
            {
                if (StateService.UseCelsius != value)
                {
                    StateService.UseCelsius = value;
                    OnPropertyChanged();  // Notify THIS ViewModel's view
                }
            }
        }

        protected ViewModelBase(IDataService dataService, IApplicationStateService stateService)
        {
            DataService = dataService;
            StateService = stateService;

            // Subscribe to changes from OTHER ViewModels
            StateService.SelectedLocationChanged += (s, location) =>
            {
                OnPropertyChanged(nameof(SelectedLocation));
            };

            StateService.TemperatureUnitChanged += (s, value) =>
            {
                OnPropertyChanged(nameof(UseCelsius));
            };
        }

        // IsBusy, ErrorMessage, ExecuteAsync, etc...
    }
}
```

**Pattern Explanation:**
1. **Get:** Returns value directly from service
2. **Set:** Sets value in service, then notifies view
3. **Event Subscription:** Listens for changes from service (other VMs)
4. **Propagation:** When service changes, all VMs get notified

---

### 4. App.xaml.cs (Registration & Initialization)

```csharp
private void ConfigureServices(IServiceCollection services)
{
    // Register as SINGLETON (only one instance)
    services.AddSingleton<IApplicationStateService, ApplicationStateService>();

    // ViewModels are TRANSIENT (new instance each time)
    services.AddTransient<DashboardViewModel>();
    services.AddTransient<HistoryViewModel>();
    
    // Other services...
}

protected override async void OnStartup(StartupEventArgs e)
{
    base.OnStartup(e);

    ConfigureServices(serviceCollection);
    ServiceProvider = serviceCollection.BuildServiceProvider();

    InitializeDatabase();

    // Initialize state BEFORE showing UI
    using (var scope = ServiceProvider.CreateScope())
    {
        var dataService = scope.ServiceProvider.GetRequiredService<IDataService>();
        var stateService = ServiceProvider.GetRequiredService<IApplicationStateService>();

        // Load defaults from database
        var defaultLocation = await dataService.GetDefaultLocationAsync();
        stateService.SelectedLocation = defaultLocation;

        var unit = await dataService.GetSettingAsync("TemperatureUnit", "Celsius");
        stateService.UseCelsius = unit == "Celsius";
    }

    // Now show UI with state already loaded
    var mainWindow = new MainWindow(ServiceProvider);
    mainWindow.Show();
}
```

**Startup Flow:**
1. Build DI container
2. Initialize database
3. **Load shared state from database**
4. Show UI (ViewModels already have state)

---

## 🔄 How It Works: Step-by-Step

### Scenario: User Selects "Kalispell" in Dashboard

```
Step 1: User clicks dropdown
    └─> XAML binding triggers

Step 2: DashboardViewModel.SelectedLocation setter called
    └─> Checks if value changed
    └─> StateService.SelectedLocation = "Kalispell"
    └─> OnPropertyChanged() ← Updates Dashboard view

Step 3: ApplicationStateService.SelectedLocation setter
    └─> _selectedLocation = "Kalispell"
    └─> Fires event: SelectedLocationChanged.Invoke(this, "Kalispell")

Step 4: ViewModelBase event subscription (in ALL ViewModels)
    └─> HistoryViewModel receives event
    └─> OnPropertyChanged(nameof(SelectedLocation)) ← Updates History view

Result:
✅ Dashboard shows Kalispell
✅ History shows Kalispell (automatically!)
✅ Both views synchronized
✅ No manual code needed
```

---

## 🎯 Benefits

### 1. True Shared State
```csharp
// NOT duplicated - same instance
var dashboard = new DashboardViewModel(..., stateService);
var history = new HistoryViewModel(..., stateService);

dashboard.SelectedLocation = Kalispell;
// history.SelectedLocation is ALSO Kalispell (same object!)
```

### 2. Automatic Synchronization
- Change in Dashboard → History updates
- Change in History → Dashboard updates
- No manual sync code
- Event-driven architecture

### 3. Clean Code
- ViewModels don't know about each other
- No tight coupling
- Easy to test
- Easy to extend

### 4. Excellent UX
- User selects location once
- All views show that location
- Temperature unit toggle affects everything
- Seamless experience

---

## 🆚 Comparison with Alternatives

### ❌ Duplicate Properties (Original Problem)

```csharp
// DashboardViewModel
private SavedLocation? _selectedLocation;

// HistoryViewModel
private SavedLocation? _selectedLocation;  // Duplicate!
```

**Problems:**
- Can get out of sync
- Manual synchronization needed
- Code duplication

---

### ❌ Static Properties

```csharp
public static SavedLocation? GlobalSelectedLocation { get; set; }
```

**Problems:**
- Not testable
- No dependency injection
- Global coupling
- No change notification

---

### ❌ Messenger/EventAggregator

```csharp
Messenger.Send(new LocationChangedMessage(Kalispell));
```

**Pros:**
- Fully decoupled

**Cons:**
- Weak typing
- No direct state access
- More complex
- Harder to debug

---

### ✅ Singleton Service (Our Solution)

```csharp
services.AddSingleton<IApplicationStateService, ApplicationStateService>();
```

**Pros:**
- ✅ Strong typing
- ✅ Testable (can mock interface)
- ✅ Direct property access
- ✅ Dependency injection
- ✅ Clear ownership
- ✅ Change notification built-in

**Cons:**
- Slight coupling (acceptable for shared state)

---

## 📊 When to Use This Pattern

### ✅ Use For:

1. **Cross-View State**
   - Selected item/location
   - Active filter
   - Current user

2. **User Preferences**
   - Theme (light/dark)
   - Units (metric/imperial)
   - Language

3. **Application Status**
   - Connection state
   - Last update time
   - Global loading state

4. **Navigation Context**
   - Current tab
   - Breadcrumb trail
   - Back stack

---

### ❌ Don't Use For:

1. **View-Specific State**
   - Form field values
   - Scroll position
   - Temporary UI state

2. **Large Data Sets**
   - Full list of items (use service queries)
   - Complete history (load on demand)
   - Cached API responses

3. **Computed Values**
   - Derived from other properties
   - Formatting strings
   - UI calculations

---

## 🧪 Testing

### Unit Test Example

```csharp
[Fact]
public void SelectedLocation_WhenChanged_FiresEvent()
{
    // Arrange
    var service = new ApplicationStateService();
    SavedLocation? capturedLocation = null;
    service.SelectedLocationChanged += (s, loc) => capturedLocation = loc;
    
    var Kalispell = new SavedLocation { Name = "Kalispell" };
    
    // Act
    service.SelectedLocation = Kalispell;
    
    // Assert
    Assert.Equal(Kalispell, service.SelectedLocation);
    Assert.Equal(Kalispell, capturedLocation);
}

[Fact]
public void ViewModels_ShareSameState()
{
    // Arrange
    var mockDataService = new Mock<IDataService>();
    var stateService = new ApplicationStateService();
    
    var dashboard = new DashboardViewModel(mockDataService.Object, stateService, ...);
    var history = new HistoryViewModel(mockDataService.Object, stateService, ...);
    
    // Act
    dashboard.SelectedLocation = new SavedLocation { Name = "Paris" };
    
    // Assert
    Assert.Equal("Paris", history.SelectedLocation?.Name);
}
```

---

## 🔧 Extension: Adding New Shared State

To add new shared state (e.g., `IsOnline`):

### 1. Add to IApplicationStateService
```csharp
bool IsOnline { get; set; }
event EventHandler<bool>? ConnectionStatusChanged;
```

### 2. Implement in ApplicationStateService
```csharp
private bool _isOnline = true;

public bool IsOnline
{
    get => _isOnline;
    set
    {
        if (_isOnline != value)
        {
            _isOnline = value;
            ConnectionStatusChanged?.Invoke(this, value);
        }
    }
}

public event EventHandler<bool>? ConnectionStatusChanged;
```

### 3. Add wrapper to ViewModelBase
```csharp
public bool IsOnline
{
    get => StateService.IsOnline;
    set
    {
        if (StateService.IsOnline != value)
        {
            StateService.IsOnline = value;
            OnPropertyChanged();
        }
    }
}

// In constructor
StateService.ConnectionStatusChanged += (s, value) =>
{
    OnPropertyChanged(nameof(IsOnline));
};
```

**Result:** All ViewModels automatically get `IsOnline` property!

---

## 💡 Key Takeaways

1. **Singleton Service** holds the actual state
2. **ViewModelBase** provides delegation properties for binding
3. **Custom Events** notify all ViewModels of changes
4. **Initialized on Startup** before UI is shown
5. **Automatic Synchronization** with no manual code
6. **Clean, Testable Architecture** following SOLID principles

---

## 🎓 Interview Talking Points

**"Tell me about your state management approach"**
> "I use a singleton ApplicationStateService to hold shared state like SelectedLocation and UseCelsius. ViewModelBase wraps this with delegation properties that get/set directly to the service. When state changes, the service fires custom events, and all ViewModels receive the notification and update their views. This provides automatic synchronization across the application with clean, testable code."

**"Why not use PropertyChanged?"**
> "I chose custom events for specificity. Each event clearly indicates what changed and carries the exact data. ViewModels can subscribe to specific events they care about, and the event signature documents the data being passed. This is clearer than a generic PropertyChanged with a string property name."

**"How do you prevent memory leaks with events?"**
> "ViewModels are transient (recreated each time), while the StateService is a singleton. However, since ViewModels don't outlive the application, and they subscribe in their constructors, there's no leak risk. If ViewModels were long-lived, I'd implement IDisposable and unsubscribe in Dispose()."

---

**Pattern Name:** Singleton State Service with Property Delegation  
**Category:** State Management  
**Difficulty:** Intermediate  
**Benefits:** High  
**Recommended:** ✅ Yes, for multi-view WPF applications
