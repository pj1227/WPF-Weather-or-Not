# Testing Guide

This document covers the test project structure, what is tested and why, how to run tests, and the test-driven development approach used from Phase 4 onward.

---

## Quick Start

```bash
# Run all tests
dotnet test

# Run with verbose output
dotnet test --verbosity normal

# Run a specific test class
dotnet test --filter "ClassName=DashboardViewModelTests"

# Run with coverage (requires coverlet)
dotnet test --collect:"XPlat Code Coverage"
```

---

## Test Project Setup

```
WPF-Weather-or-Not/
├── src/
│   └── WeatherDashboard/               ← Production code
└── tests/
    └── WeatherDashboard.Tests/
        ├── WeatherDashboard.Tests.csproj
        ├── ApplicationStateServiceTests.cs
        ├── TemperatureFormatterTests.cs
        ├── DashboardViewModelTests.cs
        ├── HistoryViewModelTests.cs
        └── TestHelpers/
            └── MockSetup.cs
```

### .csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <!-- WPF types (ObservableObject etc.) require Windows TFM -->
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="xunit"                          Version="2.9.*" />
    <PackageReference Include="xunit.runner.visualstudio"      Version="2.8.*" />
    <PackageReference Include="Moq"                            Version="4.20.*" />
    <PackageReference Include="Microsoft.NET.Test.Sdk"         Version="17.*" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\WeatherDashboard\WeatherDashboard.csproj" />
  </ItemGroup>
</Project>
```

---

## Propagation Mechanism — What Tests Must Simulate

The temperature unit toggle propagates through `INotifyPropertyChanged`, not a custom event. The chain is:

1. `ApplicationStateService.UseCelsius` setter calls `SetProperty` → fires `PropertyChanged("UseCelsius")`
2. `ViewModelBase.StateService_PropertyChanged` receives it → calls `OnTemperatureUnitChanged()`
3. Derived ViewModel's `OnTemperatureUnitChanged()` override runs

To simulate this in tests, raise `PropertyChanged` on the mock directly:

```csharp
// Simulate ApplicationStateService firing PropertyChanged("UseCelsius")
mockState.Object.UseCelsius = false;
mockState.Raise(
    s => s.PropertyChanged += null,
    new PropertyChangedEventArgs(nameof(IApplicationStateService.UseCelsius)));
```

This is the correct and complete way to trigger the unit toggle in tests. It requires `IApplicationStateService : INotifyPropertyChanged` (which it does) — no custom event on the interface is needed.

---

## TestHelpers/MockSetup.cs

```csharp
using System.ComponentModel;
using Moq;
using WeatherDashboard.Data.Entities;
using WeatherDashboard.Services.Interfaces;

public static class MockSetup
{
    public static Mock<IApplicationStateService> CreateStateService(bool useCelsius = true)
    {
        var mock = new Mock<IApplicationStateService>();
        mock.SetupProperty(s => s.UseCelsius, useCelsius);
        mock.SetupProperty(s => s.SelectedLocation, (SavedLocation?)null);
        return mock;
    }

    public static Mock<IDataService> CreateDataService()
    {
        var mock = new Mock<IDataService>();
        mock.Setup(d => d.GetSettingAsync(It.IsAny<string>()))
            .ReturnsAsync((string?)null);
        mock.Setup(d => d.GetSettingAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((string key, string def) => def);
        mock.Setup(d => d.GetAllLocationsAsync())
            .ReturnsAsync(new List<SavedLocation>());
        return mock;
    }

    public static Mock<IWeatherService> CreateWeatherService()
        => new Mock<IWeatherService>();

    /// <summary>
    /// Simulates a user toggling the unit or the startup preference being loaded.
    /// Sets the value on the mock and raises PropertyChanged("UseCelsius") so
    /// ViewModelBase.StateService_PropertyChanged fires and calls OnTemperatureUnitChanged().
    /// </summary>
    public static void RaiseUnitChanged(Mock<IApplicationStateService> mockState, bool useCelsius)
    {
        mockState.Object.UseCelsius = useCelsius;
        mockState.Raise(
            s => s.PropertyChanged += null,
            new PropertyChangedEventArgs(nameof(IApplicationStateService.UseCelsius)));
    }
}
```

---

## TemperatureFormatterTests.cs

The simplest tests — pure functions with no dependencies. These establish the conversion contract that all ViewModel tests rely on.

```csharp
public class TemperatureFormatterTests
{
    [Theory]
    [InlineData(0,    32.0)]    // freezing
    [InlineData(100,  212.0)]   // boiling
    [InlineData(-40,  -40.0)]   // crossover
    [InlineData(20,   68.0)]    // comfortable room temp
    [InlineData(37,   98.6)]    // body temperature
    public void ToFahrenheit_KnownValues_ConvertsCorrectly(double celsius, double expectedF)
    {
        var result = TemperatureFormatter.ToFahrenheit(celsius);
        Assert.Equal(expectedF, result, precision: 1);
    }

    [Fact]
    public void Format_Celsius_IncludesCUnit()
    {
        var result = TemperatureFormatter.Format(20.0, useCelsius: true);
        Assert.Equal("20.0°C", result);
    }

    [Fact]
    public void Format_Fahrenheit_ConvertsAndIncludesFUnit()
    {
        // 0°C = 32°F
        var result = TemperatureFormatter.Format(0.0, useCelsius: false);
        Assert.Equal("32.0°F", result);
    }

    [Fact]
    public void Format_Fahrenheit_BodyTemp()
    {
        // 37°C = 98.6°F
        var result = TemperatureFormatter.Format(37.0, useCelsius: false);
        Assert.Equal("98.6°F", result);
    }
}
```

---

## ApplicationStateServiceTests.cs

Tests the concrete `ApplicationStateService` directly.

```csharp
public class ApplicationStateServiceTests
{
    [Fact]
    public void UseCelsius_DefaultsToTrue()
    {
        var svc = new ApplicationStateService();
        Assert.True(svc.UseCelsius);
    }

    [Fact]
    public void SetUseCelsius_DifferentValue_FiresPropertyChanged()
    {
        var svc = new ApplicationStateService();
        var changed = new List<string?>();
        svc.PropertyChanged += (s, e) => changed.Add(e.PropertyName);

        svc.UseCelsius = false;

        Assert.Contains(nameof(svc.UseCelsius), changed);
    }

    [Fact]
    public void SetUseCelsius_SameValue_DoesNotFirePropertyChanged()
    {
        var svc = new ApplicationStateService();  // defaults to true
        int count = 0;
        svc.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(svc.UseCelsius)) count++;
        };

        svc.UseCelsius = true;  // same value — should not fire

        Assert.Equal(0, count);
    }

    [Fact]
    public void SetSelectedLocation_FiresPropertyChanged()
    {
        var svc = new ApplicationStateService();
        var changed = new List<string?>();
        svc.PropertyChanged += (s, e) => changed.Add(e.PropertyName);

        svc.SelectedLocation = new SavedLocation { Id = 1, Name = "London" };

        Assert.Contains(nameof(svc.SelectedLocation), changed);
    }
}
```

---

## DashboardViewModelTests.cs

```csharp
public class DashboardViewModelTests
{
    private DashboardViewModel CreateVm(bool useCelsius,
        out Mock<IApplicationStateService> mockState)
    {
        mockState = MockSetup.CreateStateService(useCelsius);
        var mockData    = MockSetup.CreateDataService();
        var mockWeather = MockSetup.CreateWeatherService();
        return new DashboardViewModel(mockData.Object, mockState.Object, mockWeather.Object);
    }

    // ── FormattedTemperature ─────────────────────────────────────────────────

    [Fact]
    public void FormattedTemperature_NullWeather_ReturnsDash()
    {
        var vm = CreateVm(true, out _);
        Assert.Equal("--°", vm.FormattedTemperature);
    }

    [Fact]
    public void FormattedTemperature_Celsius_FormatsCorrectly()
    {
        var vm = CreateVm(true, out _);
        vm.CurrentWeather = new WeatherData { Temperature = 20.0, FeelsLike = 18.0 };
        Assert.Equal("20.0°C", vm.FormattedTemperature);
    }

    [Fact]
    public void FormattedTemperature_Fahrenheit_ConvertsAndFormats()
    {
        var vm = CreateVm(false, out _);
        vm.CurrentWeather = new WeatherData { Temperature = 0.0, FeelsLike = -5.0 };
        Assert.Equal("32.0°F", vm.FormattedTemperature);
    }

    // ── Unit toggle via PropertyChanged propagation ──────────────────────────

    [Fact]
    public void ToggleToFahrenheit_FormattedTemperature_Updates()
    {
        var vm = CreateVm(true, out var mockState);
        vm.CurrentWeather = new WeatherData { Temperature = 100.0, FeelsLike = 95.0 };

        MockSetup.RaiseUnitChanged(mockState, false);

        Assert.Equal("212.0°F", vm.FormattedTemperature);
    }

    [Fact]
    public void ToggleBackToCelsius_FormattedTemperature_Reverts()
    {
        var vm = CreateVm(false, out var mockState);
        vm.CurrentWeather = new WeatherData { Temperature = 100.0, FeelsLike = 95.0 };

        MockSetup.RaiseUnitChanged(mockState, true);

        Assert.Equal("100.0°C", vm.FormattedTemperature);
    }

    [Fact]
    public void ToggleToFahrenheit_FormattedFeelsLike_Updates()
    {
        var vm = CreateVm(true, out var mockState);
        vm.CurrentWeather = new WeatherData { Temperature = 0.0, FeelsLike = -5.0 };

        MockSetup.RaiseUnitChanged(mockState, false);

        // -5°C = 23°F
        Assert.Equal("23.0°F", vm.FormattedFeelsLike);
    }

    // ── PropertyChanged raised on toggle ─────────────────────────────────────

    [Fact]
    public void ToggleUnit_RaisesPropertyChangedForFormattedTemperature()
    {
        var vm = CreateVm(true, out var mockState);
        vm.CurrentWeather = new WeatherData { Temperature = 20.0, FeelsLike = 18.0 };

        var raised = new List<string?>();
        vm.PropertyChanged += (s, e) => raised.Add(e.PropertyName);

        MockSetup.RaiseUnitChanged(mockState, false);

        Assert.Contains(nameof(vm.FormattedTemperature), raised);
        Assert.Contains(nameof(vm.FormattedFeelsLike), raised);
    }

    // ── Forecast TempDisplay stamping ────────────────────────────────────────

    [Fact]
    public void ToggleToFahrenheit_ForecastItems_TempMaxDisplay_Updates()
    {
        var vm = CreateVm(true, out var mockState);
        vm.Forecast = new ObservableCollection<ForecastData>
        {
            new ForecastData { TempMax = 0.0, TempMin = -10.0,
                               TempMaxDisplay = 0.0, TempMinDisplay = -10.0 }
        };

        MockSetup.RaiseUnitChanged(mockState, false);

        Assert.Equal(32.0, vm.Forecast[0].TempMaxDisplay, precision: 1);   // 0°C = 32°F
        Assert.Equal(14.0, vm.Forecast[0].TempMinDisplay, precision: 1);   // -10°C = 14°F
    }

    [Fact]
    public void ToggleBackToCelsius_ForecastItems_TempMaxDisplay_Reverts()
    {
        var vm = CreateVm(false, out var mockState);
        vm.Forecast = new ObservableCollection<ForecastData>
        {
            new ForecastData { TempMax = 0.0, TempMin = -10.0,
                               TempMaxDisplay = 32.0, TempMinDisplay = 14.0 }
        };

        MockSetup.RaiseUnitChanged(mockState, true);

        Assert.Equal(0.0,   vm.Forecast[0].TempMaxDisplay, precision: 1);
        Assert.Equal(-10.0, vm.Forecast[0].TempMinDisplay, precision: 1);
    }
}
```

---

## HistoryViewModelTests.cs

```csharp
public class HistoryViewModelTests
{
    private HistoryViewModel CreateVm(bool useCelsius,
        out Mock<IApplicationStateService> mockState)
    {
        mockState = MockSetup.CreateStateService(useCelsius);
        var mockData   = MockSetup.CreateDataService();
        var mockReport = new Mock<IReportService>();
        return new HistoryViewModel(mockData.Object, mockState.Object, mockReport.Object);
    }

    private static List<WeatherRecord> SampleHistory() => new()
    {
        new WeatherRecord { Temperature = 0,  FeelsLike = -2, Humidity = 80,
                            Timestamp = DateTime.Now.AddDays(-2) },
        new WeatherRecord { Temperature = 10, FeelsLike = 8,  Humidity = 60,
                            Timestamp = DateTime.Now.AddDays(-1) },
        new WeatherRecord { Temperature = 20, FeelsLike = 19, Humidity = 40,
                            Timestamp = DateTime.Now },
    };

    // ── Stats in Celsius ─────────────────────────────────────────────────────

    [Fact]
    public void AverageTemperature_Celsius_ReturnsCorrectAverage()
    {
        var vm = CreateVm(true, out _);
        vm.WeatherHistory = SampleHistory();
        Assert.Equal(10.0, vm.AverageTemperature, precision: 1);
    }

    [Fact]
    public void MaxTemperature_Celsius_ReturnsCorrectMax()
    {
        var vm = CreateVm(true, out _);
        vm.WeatherHistory = SampleHistory();
        Assert.Equal(20.0, vm.MaxTemperature, precision: 1);
    }

    [Fact]
    public void MinTemperature_Celsius_ReturnsCorrectMin()
    {
        var vm = CreateVm(true, out _);
        vm.WeatherHistory = SampleHistory();
        Assert.Equal(0.0, vm.MinTemperature, precision: 1);
    }

    // ── Stats convert on toggle ───────────────────────────────────────────────

    [Fact]
    public void AverageTemperature_AfterToggleToFahrenheit_ConvertsCorrectly()
    {
        var vm = CreateVm(true, out var mockState);
        vm.WeatherHistory = SampleHistory();  // avg = 10°C

        MockSetup.RaiseUnitChanged(mockState, false);

        Assert.Equal(50.0, vm.AverageTemperature, precision: 1);  // 10°C = 50°F
    }

    [Fact]
    public void MaxTemperature_AfterToggleToFahrenheit_ConvertsCorrectly()
    {
        var vm = CreateVm(true, out var mockState);
        vm.WeatherHistory = SampleHistory();  // max = 20°C

        MockSetup.RaiseUnitChanged(mockState, false);

        Assert.Equal(68.0, vm.MaxTemperature, precision: 1);  // 20°C = 68°F
    }

    [Fact]
    public void MinTemperature_AfterToggleToFahrenheit_ConvertsCorrectly()
    {
        var vm = CreateVm(true, out var mockState);
        vm.WeatherHistory = SampleHistory();  // min = 0°C

        MockSetup.RaiseUnitChanged(mockState, false);

        Assert.Equal(32.0, vm.MinTemperature, precision: 1);  // 0°C = 32°F
    }

    [Fact]
    public void AverageTemperature_EmptyHistory_ReturnsZero()
    {
        var vm = CreateVm(true, out _);
        Assert.Equal(0.0, vm.AverageTemperature);
    }

    // ── PropertyChanged raised on toggle ─────────────────────────────────────

    [Fact]
    public void ToggleUnit_RaisesPropertyChangedForStats()
    {
        var vm = CreateVm(true, out var mockState);
        vm.WeatherHistory = SampleHistory();

        var raised = new List<string?>();
        vm.PropertyChanged += (s, e) => raised.Add(e.PropertyName);

        MockSetup.RaiseUnitChanged(mockState, false);

        Assert.Contains(nameof(vm.AverageTemperature), raised);
        Assert.Contains(nameof(vm.MaxTemperature),     raised);
        Assert.Contains(nameof(vm.MinTemperature),     raised);
    }

    // ── Chart rebuild on toggle ───────────────────────────────────────────────

    [Fact]
    public void ToggleUnit_WithHistory_RaisesPropertyChangedForTemperaturePlot()
    {
        var vm = CreateVm(true, out var mockState);
        vm.WeatherHistory = SampleHistory();

        var raised = new List<string?>();
        vm.PropertyChanged += (s, e) => raised.Add(e.PropertyName);

        MockSetup.RaiseUnitChanged(mockState, false);

        Assert.Contains(nameof(vm.TemperaturePlot), raised);
    }

    [Fact]
    public void ToggleUnit_WithNoHistory_DoesNotRaiseTemperaturePlot()
    {
        var vm = CreateVm(true, out var mockState);
        // WeatherHistory is empty — chart rebuild is guarded by !WeatherHistory.Any()

        var raised = new List<string?>();
        vm.PropertyChanged += (s, e) => raised.Add(e.PropertyName);

        MockSetup.RaiseUnitChanged(mockState, false);

        // Stats are re-raised (they return 0 safely), but chart should not rebuild
        Assert.DoesNotContain(nameof(vm.TemperaturePlot), raised);
    }
}
```

---

## ReportServiceTests.cs

`ReportService` receives `IApplicationStateService` in its constructor, so it can be tested the same way as the ViewModels — mock the state service, set `UseCelsius`, and verify the output.

Because `GeneratePdfReportAsync` and `GenerateExcelReportAsync` both produce binary output (PDF bytes / Excel bytes), the most practical approach is to verify the service runs without throwing and then inspect specific numeric values rather than parsing the binary formats. For the Excel case this is straightforward because EPPlus gives us the worksheet cells directly if we unpack the bytes back into a package.

```csharp
public class ReportServiceTests
{
    private ReportService CreateService(bool useCelsius,
        out Mock<IApplicationStateService> mockState,
        out Mock<IDataService> mockData)
    {
        mockState = MockSetup.CreateStateService(useCelsius);
        mockData  = MockSetup.CreateDataService();
        return new ReportService(mockData.Object, mockState.Object);
    }

    private static List<WeatherRecord> SampleRecords() => new()
    {
        new WeatherRecord
        {
            LocationId  = 1,
            Timestamp   = DateTime.Now.AddDays(-2),
            Temperature = 0.0,    // 32°F
            FeelsLike   = -5.0,   // 23°F
            Humidity    = 80,
            Pressure    = 1013,
            WindSpeed   = 3.5,
            Description = "Clear"
        },
        new WeatherRecord
        {
            LocationId  = 1,
            Timestamp   = DateTime.Now.AddDays(-1),
            Temperature = 20.0,   // 68°F
            FeelsLike   = 18.0,   // 64.4°F
            Humidity    = 60,
            Pressure    = 1010,
            WindSpeed   = 5.0,
            Description = "Cloudy"
        },
    };

    private static SavedLocation SampleLocation() =>
        new SavedLocation { Id = 1, Name = "London", Country = "GB" };

    // ── Excel conversion ─────────────────────────────────────────────────────

    [Fact]
    public async Task GenerateExcelReport_Celsius_TemperatureValuesAreRaw()
    {
        var service = CreateService(true, out _, out var mockData);
        mockData.Setup(d => d.GetLocationByIdAsync(1)).ReturnsAsync(SampleLocation());
        mockData.Setup(d => d.GetWeatherHistoryAsync(1, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(SampleRecords());

        var bytes = await service.GenerateExcelReportAsync(1, DateTime.Now.AddDays(-7), DateTime.Now);

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var pkg = new ExcelPackage(new System.IO.MemoryStream(bytes));
        var ws = pkg.Workbook.Worksheets["Weather Data"];

        // Row 12 is first data row (headerRow=11, data starts at 12)
        var temp = (double)ws.Cells[12, 2].Value;
        Assert.Equal(0.0, temp, precision: 1);   // raw Celsius, unchanged
    }

    [Fact]
    public async Task GenerateExcelReport_Fahrenheit_TemperatureValuesAreConverted()
    {
        var service = CreateService(false, out _, out var mockData);
        mockData.Setup(d => d.GetLocationByIdAsync(1)).ReturnsAsync(SampleLocation());
        mockData.Setup(d => d.GetWeatherHistoryAsync(1, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(SampleRecords());

        var bytes = await service.GenerateExcelReportAsync(1, DateTime.Now.AddDays(-7), DateTime.Now);

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var pkg = new ExcelPackage(new System.IO.MemoryStream(bytes));
        var ws = pkg.Workbook.Worksheets["Weather Data"];

        // 0°C = 32°F; first data row, Temperature column (col 2)
        var temp = (double)ws.Cells[12, 2].Value;
        Assert.Equal(32.0, temp, precision: 1);
    }

    [Fact]
    public async Task GenerateExcelReport_Celsius_ColumnHeaderContainsCelsiusLabel()
    {
        var service = CreateService(true, out _, out var mockData);
        mockData.Setup(d => d.GetLocationByIdAsync(1)).ReturnsAsync(SampleLocation());
        mockData.Setup(d => d.GetWeatherHistoryAsync(1, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(SampleRecords());

        var bytes = await service.GenerateExcelReportAsync(1, DateTime.Now.AddDays(-7), DateTime.Now);

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var pkg = new ExcelPackage(new System.IO.MemoryStream(bytes));
        var ws = pkg.Workbook.Worksheets["Weather Data"];

        var header = ws.Cells[11, 2].Value?.ToString();
        Assert.Contains("\u00b0C", header);   // °C
    }

    [Fact]
    public async Task GenerateExcelReport_Fahrenheit_ColumnHeaderContainsFahrenheitLabel()
    {
        var service = CreateService(false, out _, out var mockData);
        mockData.Setup(d => d.GetLocationByIdAsync(1)).ReturnsAsync(SampleLocation());
        mockData.Setup(d => d.GetWeatherHistoryAsync(1, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(SampleRecords());

        var bytes = await service.GenerateExcelReportAsync(1, DateTime.Now.AddDays(-7), DateTime.Now);

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var pkg = new ExcelPackage(new System.IO.MemoryStream(bytes));
        var ws = pkg.Workbook.Worksheets["Weather Data"];

        var header = ws.Cells[11, 2].Value?.ToString();
        Assert.Contains("\u00b0F", header);   // °F
    }

    [Fact]
    public async Task GenerateExcelReport_Fahrenheit_StatAvgIsConverted()
    {
        var service = CreateService(false, out _, out var mockData);
        mockData.Setup(d => d.GetLocationByIdAsync(1)).ReturnsAsync(SampleLocation());
        mockData.Setup(d => d.GetWeatherHistoryAsync(1, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(SampleRecords());

        var bytes = await service.GenerateExcelReportAsync(1, DateTime.Now.AddDays(-7), DateTime.Now);

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var pkg = new ExcelPackage(new System.IO.MemoryStream(bytes));
        var ws = pkg.Workbook.Worksheets["Weather Data"];

        // avg of (0°C, 20°C) = 10°C = 50°F
        var avg = (double)ws.Cells[6, 2].Value;
        Assert.Equal(50.0, avg, precision: 1);
    }

    // ── PDF — smoke tests (binary output, limited introspection) ─────────────

    [Fact]
    public async Task GeneratePdfReport_Celsius_ReturnsNonEmptyBytes()
    {
        var service = CreateService(true, out _, out var mockData);
        mockData.Setup(d => d.GetLocationByIdAsync(1)).ReturnsAsync(SampleLocation());
        mockData.Setup(d => d.GetWeatherHistoryAsync(1, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(SampleRecords());

        var bytes = await service.GeneratePdfReportAsync(1, DateTime.Now.AddDays(-7), DateTime.Now);

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);
    }

    [Fact]
    public async Task GeneratePdfReport_Fahrenheit_ReturnsNonEmptyBytes()
    {
        var service = CreateService(false, out _, out var mockData);
        mockData.Setup(d => d.GetLocationByIdAsync(1)).ReturnsAsync(SampleLocation());
        mockData.Setup(d => d.GetWeatherHistoryAsync(1, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(SampleRecords());

        var bytes = await service.GeneratePdfReportAsync(1, DateTime.Now.AddDays(-7), DateTime.Now);

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);
    }

    [Fact]
    public async Task GenerateReport_NoData_ThrowsException()
    {
        var service = CreateService(true, out _, out var mockData);
        mockData.Setup(d => d.GetLocationByIdAsync(1)).ReturnsAsync(SampleLocation());
        mockData.Setup(d => d.GetWeatherHistoryAsync(1, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new List<WeatherRecord>());

        await Assert.ThrowsAsync<Exception>(() =>
            service.GeneratePdfReportAsync(1, DateTime.Now.AddDays(-7), DateTime.Now));
    }
}
```

> **Note:** The Excel tests use EPPlus directly to inspect cell values from the generated bytes — add `EPPlus` as a test project package reference if it isn't already transitive from the production project reference.

---

## Running in CI

```yaml
# .github/workflows/ci.yml
name: CI

on: [push, pull_request]

jobs:
  test:
    runs-on: windows-latest   # WPF TFM (net8.0-windows) requires Windows
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      - run: dotnet restore
      - run: dotnet build --no-restore
      - run: dotnet test --no-build --verbosity normal
```

---

## Phase 4 TDD Approach

All Phase 4 features follow this cycle:

```
1. Write a failing test describing the desired behaviour
2. Run — confirm it fails for the right reason
3. Write minimum code to make it pass
4. Run all tests — confirm no regressions
5. Refactor if needed — tests stay green
6. Commit: tests and implementation together
```

### What to test vs not test

**Test:**
- All temperature conversion paths (both directions, edge cases)
- All `PropertyChanged` propagation paths through `ViewModelBase`
- `OnTemperatureUnitChanged()` override behaviour in each ViewModel
- Command `CanExecute` guards
- `ExecuteAsync` error handling and `IsBusy` state
- Any business rule in a ViewModel

**Don't test:**
- XAML layout and visual output
- ScottPlot rendering
- EF Core migrations
- `HttpClient` calls (mock `IWeatherService`)
- WPF data binding mechanics (framework's responsibility)

### Naming convention

```
MethodOrProperty_Scenario_ExpectedResult

Examples:
  FormattedTemperature_NullWeather_ReturnsDash
  AverageTemperature_AfterToggleToFahrenheit_ConvertsCorrectly
  SetUseCelsius_SameValue_DoesNotFirePropertyChanged
  ToggleUnit_WithNoHistory_DoesNotRaiseTemperaturePlot
```

---

## Coverage Goals

| Area | Target | Rationale |
|---|---|---|
| `TemperatureFormatter` | 100% | Core math — zero tolerance for errors |
| `ApplicationStateService` | 100% | Core state contract |
| `DashboardViewModel` display strings | 90%+ | All format paths including null guards |
| `DashboardViewModel` forecast stamping | 90%+ | Both load-time and toggle-time paths |
| `HistoryViewModel` stat conversion | 90%+ | All unit paths, empty history guard |
| `HistoryViewModel` chart rebuild guard | 80%+ | With and without history |
| `ReportService` Excel conversion | 90%+ | Values and labels for both units; empty data guard |
| `ReportService` PDF | smoke tests | Binary output limits introspection; verify it runs and returns bytes |
| `ViewModelBase` | 80%+ | `ExecuteAsync`, `IsBusy` toggling |
| XAML / code-behind | 0% | UI tests deferred to Phase 5 (consider FlaUI or Appium) |
