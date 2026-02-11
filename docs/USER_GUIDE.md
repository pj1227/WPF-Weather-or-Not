# Weather Dashboard - User Guide

## Welcome! 🌤️

Weather Dashboard is your personal weather tracking application. This guide will help you get the most out of all features.

---

## Table of Contents

1. [Getting Started](#getting-started)
2. [Dashboard - Current Weather](#dashboard---current-weather)
3. [History - Weather Trends](#history---weather-trends)
4. [Settings & Preferences](#settings--preferences)
5. [Exporting Reports](#exporting-reports)
6. [Tips & Tricks](#tips--tricks)
7. [Troubleshooting](#troubleshooting)

---

## Getting Started

### First Launch

When you first open Weather Dashboard, you'll need to:

1. **Enter your API Key** (one-time setup)
   - The app will prompt you
   - Or add it in the database manually
   - Get a free key from [OpenWeatherMap](https://openweathermap.org/api)

2. **Search for your first location**
   - Type a city name in the search box
   - Click "Search Location"
   - The weather will load automatically

3. **Set your default location**
   - Click "Set as Default Location"
   - This location will load automatically next time

4. **Choose your temperature units**
   - Click the temperature unit button (°C / °F)
   - Your preference is saved automatically

---

## Dashboard - Current Weather

The Dashboard is your main view for checking current weather and forecasts.

### Current Weather Display

**What You See:**
- **Location Name** - City and country
- **Current Temperature** - Large display with current temp
- **Weather Description** - Current conditions (e.g., "Clear sky")
- **Last Updated** - When data was last refreshed

**Weather Details:**
- **Feels Like** - Apparent temperature
- **Humidity** - Moisture level (%)
- **Wind Speed** - Current wind (m/s)
- **Pressure** - Atmospheric pressure (hPa)

### 5-Day Forecast

See the next 5 days at a glance:
- **Date** - Day of week and date
- **Conditions** - Weather description
- **High/Low** - Daily temperature range

### How to Use

**Search for a New Location:**
1. Type city name in search box
2. Press Enter or click "Search Location"
3. Weather loads automatically
4. Location is saved to your list

**Switch Between Saved Locations:**
1. Click the dropdown next to the search box
2. Select any saved location
3. Weather updates automatically

**Refresh Current Data:**
- Click the "Refresh" button
- Data is fetched from the weather service
- Saved to your history

**Change Temperature Units:**
- Click the unit button (shows °C or °F)
- All temperatures update immediately
- Setting is saved for next time

**Set Default Location:**
1. Select your preferred location
2. Click "Set as Default Location"
3. This location loads when you start the app

---

## History - Weather Trends

View and analyze your weather data over time with interactive charts.

### What You Can Do

- **View temperature trends** - See how temperature has changed
- **Analyze humidity patterns** - Track humidity over time
- **Compare periods** - Filter by date range
- **Generate statistics** - See averages, highs, and lows
- **Export reports** - Save data as PDF or Excel

### Using the History View

**Select a Location:**
1. Click the "History" tab
2. Choose a location from the dropdown
3. Data loads automatically

**Change Date Range:**
1. Click the "Start Date" picker
2. Select your starting date
3. Click the "End Date" picker
4. Select your ending date
5. Charts update automatically

**Reading the Charts:**

**Temperature Chart:**
- Red line = Actual temperature
- Orange dashed line = "Feels like" temperature
- X-axis = Time (dates)
- Y-axis = Temperature (in your chosen units)

**Humidity Chart:**
- Blue line = Humidity percentage
- Gray dotted line = 50% reference
- X-axis = Time (dates)
- Y-axis = Humidity (0-100%)

**Statistics Summary:**
- **Average Temperature** - Mean temp for period
- **Maximum Temperature** - Highest recorded temp
- **Minimum Temperature** - Lowest recorded temp
- **Average Humidity** - Mean humidity percentage

### Interacting with Charts

**Zoom:**
- Scroll mouse wheel to zoom in/out
- Charts must have data to interact

**Pan:**
- Click and drag to move around
- View different parts of the timeline

**Reset:**
- Change date range to reset view
- Or select different location

---

## Settings & Preferences

### Temperature Units

**Celsius (°C) or Fahrenheit (°F):**
- Click the unit button on Dashboard
- Changes immediately
- Affects all temperature displays
- Saved between sessions

### Default Location

**Set Your Preferred Location:**
1. Select a location from dropdown
2. Click "Set as Default Location"
3. Green confirmation message appears
4. This location loads automatically on startup

**Why Set a Default:**
- Saves time - no need to search each time
- Gets latest weather immediately
- Your most-used location is always ready

---

## Exporting Reports

Generate professional reports for any location and date range.

### PDF Reports

**What's Included:**
- Location and date range
- Summary statistics
- Detailed weather records
- Professional formatting

**How to Export:**
1. Go to History tab
2. Select location and date range
3. Click "Export to PDF"
4. Wait for "Report saved" message
5. File saved to your Documents folder

**File Naming:**
`WeatherReport_[LocationName]_[Date].pdf`

Example: `WeatherReport_London_20250212.pdf`

### Excel Reports

**What's Included:**
- All weather data points
- Organized in spreadsheet format
- Ready for further analysis
- Charts can be added in Excel

**How to Export:**
1. Go to History tab
2. Select location and date range
3. Click "Export to Excel"
4. Wait for "Report saved" message
5. File saved to your Documents folder

**File Naming:**
`WeatherReport_[LocationName]_[Date].xlsx`

Example: `WeatherReport_NewYork_20250212.xlsx`

### Finding Your Reports

**Location:**
Windows: `C:\Users\[YourName]\Documents\`

Look for files starting with "WeatherReport_"

---

## Tips & Tricks

### Maximize Your Data Collection

**Collect More Historical Data:**
- Search locations you care about
- Click refresh periodically
- The more data you collect, the better your trends!

**Best Practices:**
- Refresh daily for continuous trends
- Keep at least 7 days for meaningful charts
- 30+ days shows seasonal patterns

### Efficient Workflow

**Quick Access:**
1. Set your home city as default
2. Add 2-3 frequently checked cities
3. Use dropdown to switch quickly

**Comparison Strategy:**
1. Export report for Location A
2. Change location to Location B
3. Export report for Location B
4. Compare in Excel or side-by-side

### Understanding the Data

**Temperature vs. Feels Like:**
- "Feels Like" accounts for wind and humidity
- Can be very different from actual temperature
- More accurate for comfort level

**Humidity:**
- Below 30%: Very dry
- 30-50%: Comfortable
- 50-70%: Humid
- Above 70%: Very humid

**Wind Speed:**
- 0-5 m/s: Light breeze
- 5-10 m/s: Moderate wind
- 10-15 m/s: Fresh wind
- Above 15 m/s: Strong wind

---

## Troubleshooting

### Common Issues

**"No data available"**

**Cause:** No historical data for selected period  
**Solution:**
- Check if you've collected data for this location
- Try a different date range
- Refresh to collect current data
- Wait a few days and check again

---

**"Location not found"**

**Cause:** City name not recognized  
**Solutions:**
- Check spelling
- Try full city name (e.g., "New York" not "NY")
- Include country if ambiguous (e.g., "Paris, France")
- Try nearby major city

---

**"Failed to load weather data"**

**Cause:** API or network issue  
**Solutions:**
- Check internet connection
- Verify API key is entered correctly
- Wait a moment and try "Refresh"
- Check if you've hit daily API limit (1,000 calls/day)

---

**Charts are empty**

**Cause:** No data for selected filters  
**Solutions:**
- Verify you selected a location
- Check date range includes collected data
- Try "Refresh" to collect new data
- Ensure location has been searched before

---

**App crashes when scrolling**

**Cause:** Mouse wheel on empty chart (fixed in latest version)  
**Solution:**
- Ensure charts have data before interacting
- Update to latest version
- Use scrollbar instead of mouse wheel

---

**Can't set API key**

**Cause:** Database not initialized  
**Solutions:**
- Run app once to create database
- Check %LOCALAPPDATA%\WeatherDashboard folder exists
- Try running as administrator
- Reinstall application

---

### Getting Help

**Error Messages:**
- Read the error message carefully
- It usually tells you what went wrong
- Red banner = error, Green banner = success

**Data Issues:**
- Remember: App collects data when you search/refresh
- Historical trends require data collection over time
- Export reports to verify data exists

**Performance:**
- Close other programs if app is slow
- Large date ranges may take longer to load
- Charts with 1000+ data points may lag slightly

---

## Keyboard Shortcuts

**Dashboard:**
- `Enter` in search box = Search location
- `F5` = Refresh (if implemented)

**General:**
- `Alt+F4` = Close application
- Click outside dropdown = Close dropdown

---

## Data Management

### Where is My Data Stored?

**Database Location:**
`%LOCALAPPDATA%\WeatherDashboard\weather.db`

Full path (example):
`C:\Users\YourName\AppData\Local\WeatherDashboard\weather.db`

**What's Stored:**
- Your saved locations
- Historical weather records
- Application settings (API key, units, default location)

### Backup Your Data

**To Backup:**
1. Close Weather Dashboard
2. Navigate to database location
3. Copy `weather.db` file
4. Save to safe location (USB drive, cloud storage)

**To Restore:**
1. Close Weather Dashboard
2. Replace `weather.db` with your backup
3. Restart Weather Dashboard

### Reset Application

**Complete Reset:**
1. Close Weather Dashboard
2. Delete the WeatherDashboard folder from `%LOCALAPPDATA%`
3. Restart Weather Dashboard
4. Enter API key again
5. Add locations again

---

## Best Practices

### Daily Use

✅ **DO:**
- Refresh at least once daily for continuous data
- Set your most-used location as default
- Use appropriate date ranges for analysis
- Export reports for important data

❌ **DON'T:**
- Delete locations you've collected data for
- Search excessively (respect API limits)
- Ignore error messages
- Forget to back up your data

### For Best Results

**Short-term Forecasting:**
- Use 5-day forecast on Dashboard
- Refresh morning and evening
- Check "Feels Like" for actual comfort

**Long-term Analysis:**
- Collect data for 30+ days
- Use History charts for trends
- Export to Excel for detailed analysis
- Compare multiple locations

**Reporting:**
- Choose meaningful date ranges
- Include context in file names
- Save reports to organized folders
- Keep backups of important reports

---

## Updates & New Features

Weather Dashboard is actively developed. Future versions may include:

- Weather alerts and notifications
- More chart types
- Weather maps
- Comparison tools
- Mobile companion app

Check GitHub repository for latest updates and releases!

---

## Support

Need help? Found a bug? Have a suggestion?

- **GitHub Issues:** [Repository URL]
- **Email:** joel1227@proton.me

---

*Enjoy tracking the weather! 🌦️*

**Weather Dashboard User Guide v1.0**  
*Last Updated: February 2025*
