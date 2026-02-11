# WPF-Weather-or-Not
A WPF weather dashboard built with .NET 8 that displays current conditions, forecasts, and historical weather data using a public API, SQLite, and the MVVM pattern. Includes charts and report generation to showcase modern desktop application practices.


![.NET](https://img.shields.io/badge/.NET-8.0-blue)
![C#](https://img.shields.io/badge/C%23-12.0-purple)
![WPF](https://img.shields.io/badge/WPF-XAML-orange)
![License](https://img.shields.io/badge/license-MIT-green)

## 📸 Screenshots

### Dashboard - Current Weather & Forecast
![Dashboard View](docs/images/dashboard.png)
*Real-time weather data with 5-day forecast for multiple locations*

### Historical Analysis with Charts
![History View](docs/images/history.png)
*Interactive temperature and humidity trends with ScottPlot visualization*

### Report Generation
![Excel Report](docs/images/excel-report.png)
*Professional PDF and Excel reports with comprehensive weather statistics*

---

## 🎯 Project Overview

Weather Dashboard is a full-featured desktop application built to showcase professional software engineering skills. It provides real-time weather information, historical data analysis, and comprehensive reporting capabilities.

**Key Highlights:**
- 🏗️ **Clean Architecture** - MVVM pattern with clear separation of concerns
- 💾 **Data Persistence** - Entity Framework Core with SQLite
- 🌐 **API Integration** - OpenWeatherMap REST API
- 📊 **Data Visualization** - Interactive charts using ScottPlot
- 📄 **Reporting** - PDF and Excel export capabilities
- ⚙️ **Dependency Injection** - Microsoft.Extensions.DependencyInjection
- 🎨 **Modern UI** - Clean, professional WPF interface

---

## ✨ Features

### Current Weather Display
- **Multi-location support** - Save and switch between multiple cities
- **Comprehensive data** - Temperature, feels like, humidity, pressure, wind speed
- **5-day forecast** - Daily high/low temperatures with weather descriptions
- **Unit conversion** - Toggle between Celsius and Fahrenheit
- **Default location** - Set preferred location for quick access

### Historical Analysis
- **Interactive charts** - Temperature and humidity trends over time
- **Date range filtering** - Analyze specific time periods
- **Statistical summaries** - Average, maximum, minimum values
- **Auto-updating** - Charts update automatically on location/date changes

### Professional Reporting
- **PDF Reports** - Formatted reports with statistics and detailed records
- **Excel Export** - Spreadsheet format for further analysis
- **Automatic naming** - Files saved with location and date information
- **Comprehensive data** - Full weather history included

### User Experience
- **Auto-loading** - Data loads automatically when locations change
- **Persistent settings** - Preferences saved between sessions
- **Error handling** - Graceful error messages with user guidance
- **Loading indicators** - Visual feedback during data operations
- **Responsive UI** - Smooth interactions and updates

---

## 🛠️ Technology Stack

### Core Technologies
- **.NET 8.0** - Modern cross-platform framework
- **C# 12** - Latest language features
- **WPF (Windows Presentation Foundation)** - Rich desktop UI framework
- **XAML** - Declarative UI markup

### Data & Storage
- **Entity Framework Core 8** - Object-relational mapper
- **SQLite** - Lightweight embedded database
- **Code-First Migrations** - Version-controlled database schema

### External Services
- **OpenWeatherMap API** - Weather data provider
- **HttpClient** - RESTful API communication
- **System.Text.Json** - JSON serialization/deserialization

### Libraries & Packages
- **CommunityToolkit.Mvvm 8.2.2** - MVVM helpers (RelayCommand, ObservableProperty)
- **ScottPlot.WPF 5.0.42** - Data visualization and charting
- **QuestPDF 2024.12.3** - PDF document generation
- **EPPlus 7.5.2** - Excel spreadsheet creation
- **Microsoft.Extensions.DependencyInjection 8.0.1** - IoC container

---

## 📐 Architecture

### MVVM Pattern
```
Views (XAML)
    ↓ ↑ (Data Binding)
ViewModels (Business Logic)
    ↓ ↑ (Method Calls)
Services (Data Access & API)
    ↓ ↑ (Entity Mapping)
Data Layer (EF Core + SQLite)
```

### Project Structure
```
WeatherDashboard/
├── Data/
│   ├── Entities/          # Database models
│   ├── WeatherDbContext   # EF Core context
│   └── Migrations/        # Database migrations
├── Models/                # Domain models
├── Services/
│   ├── Interfaces/        # Service contracts
│   ├── WeatherApiService  # API integration
│   ├── DataService        # Database operations
│   └── ReportService      # PDF/Excel generation
├── ViewModels/            # MVVM view models
├── Views/                 # XAML user controls
└── Converters/            # Value converters
```

### Design Patterns
- **MVVM (Model-View-ViewModel)** - Separation of UI and business logic
- **Repository Pattern** - Abstracted data access
- **Dependency Injection** - Loose coupling and testability
- **Command Pattern** - User interactions via RelayCommand
- **Observer Pattern** - INotifyPropertyChanged for data binding

---

## 🚀 Getting Started

### Prerequisites
- **Visual Studio 2022** or later (Community Edition is fine)
- **.NET 8.0 SDK** ([Download](https://dotnet.microsoft.com/download/dotnet/8.0))
- **Git** (for cloning the repository)
- **OpenWeatherMap API Key** (free tier available)

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/weather-dashboard.git
   cd weather-dashboard
   ```

2. **Restore NuGet packages**
   ```bash
   dotnet restore
   ```

3. **Get an API key**
   - Sign up at [OpenWeatherMap](https://openweathermap.org/api)
   - Navigate to API Keys section
   - Copy your API key

4. **Configure the API key**
   
   The application will prompt for an API key on first run, or you can add it directly to the database:
   
   **Option A:** Let the app prompt you (recommended)
   - Just run the app, and it will guide you through setup
   
   **Option B:** Add manually to database
   - Database location: `%LOCALAPPDATA%\WeatherDashboard\weather.db`
   - Use DB Browser for SQLite or similar tool
   - Add to `UserSettings` table: `Key='ApiKey'`, `Value='YOUR_API_KEY'`

5. **Build and run**
   ```bash
   dotnet build
   dotnet run
   ```

---

## 📖 Usage Guide

### First Time Setup
1. **Launch the application**
2. **Enter your API key** when prompted (one-time setup)
3. **Search for a location** using the search box
4. **Set as default** using the "Set as Default Location" button
5. **Choose temperature units** (°C or °F)

### Daily Use
1. **Dashboard Tab** - View current weather and 5-day forecast
2. **History Tab** - Analyze temperature and humidity trends
3. **Export reports** - Generate PDF or Excel reports for any date range
4. **Switch locations** - Use dropdown to view different cities

### Tips
- The app collects weather data each time you search or refresh
- Historical charts require data to be collected over time
- Export reports for sharing with others
- Set your most-used location as default for quick access

---

## 💡 Key Learning Outcomes

This project demonstrates proficiency in:

### Software Architecture
- ✅ MVVM pattern implementation
- ✅ Separation of concerns
- ✅ Dependency injection configuration
- ✅ Service-oriented architecture

### Data Management
- ✅ Entity Framework Core code-first approach
- ✅ Database migrations
- ✅ Async data access patterns
- ✅ Complex LINQ queries

### API Integration
- ✅ RESTful API consumption
- ✅ JSON deserialization
- ✅ Error handling and retry logic
- ✅ HTTP client configuration

### User Interface
- ✅ XAML data binding
- ✅ Custom value converters
- ✅ Responsive layouts
- ✅ Professional UI/UX design

### Advanced Features
- ✅ Interactive data visualization
- ✅ Document generation (PDF/Excel)
- ✅ File I/O operations
- ✅ Configuration management

---

## 🔮 Future Enhancements

### Phase 4: Advanced Features (Planned)
- [ ] **Weather Alerts** - Notifications for severe weather
- [ ] **Multi-day Trends** - Week/month view with averages
- [ ] **Location Search** - Geographic search with autocomplete
- [ ] **Weather Maps** - Visual weather map overlay
- [ ] **Favorites System** - Star frequently accessed locations

### Phase 5: Cloud Integration (Planned)
- [ ] **Cloud Sync** - Sync locations across devices
- [ ] **User Accounts** - Personal profiles and preferences
- [ ] **Shared Reports** - Share reports via link
- [ ] **Mobile Companion** - Companion mobile app

### Performance Optimizations
- [ ] **Data Caching** - Reduce API calls with intelligent caching
- [ ] **Background Updates** - Auto-refresh in background
- [ ] **Lazy Loading** - Load historical data on demand
- [ ] **Chart Optimization** - Virtualization for large datasets

### UI Enhancements
- [ ] **Dark Mode** - Theme switching
- [ ] **Weather Icons** - Custom weather condition icons
- [ ] **Animations** - Smooth transitions and effects
- [ ] **Accessibility** - Screen reader support, keyboard navigation

---

## 📊 Technical Metrics

- **Lines of Code:** ~3,500
- **Code Files:** 25+
- **XAML Files:** 6
- **NuGet Packages:** 9
- **Database Tables:** 3
- **API Endpoints Used:** 2
- **Development Time:** ~25 hours
- **Test Coverage:** Unit tests in progress

---

## 🤝 Contributing

This is a portfolio project, but suggestions and feedback are welcome!

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 🙏 Acknowledgments

- **OpenWeatherMap** - Weather data API
- **ScottPlot Team** - Excellent charting library
- **QuestPDF** - PDF generation framework
- **EPPlus** - Excel manipulation library
- **Microsoft** - .NET platform and documentation

---

## 📧 Contact

**Joel Cossins**  
Email: joel1227@proton.me  
GitHub: [@yourusername](https://github.com/yourusername)  
LinkedIn: [Your Profile](https://linkedin.com/in/yourprofile)

---

## 🎓 For Employers

This project demonstrates:
- **Professional coding standards** - Clean, maintainable code
- **Modern development practices** - Git, async/await, SOLID principles
- **Problem-solving skills** - API integration, data persistence, visualization
- **Full-stack capabilities** - UI, business logic, data access, external services
- **Documentation** - Well-documented code and comprehensive README

**Available for full-time software development positions.**  
**Open to remote opportunities.**