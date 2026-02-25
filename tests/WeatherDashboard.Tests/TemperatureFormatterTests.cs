using WeatherDashboard.Helpers;
using Xunit;

namespace WeatherDashboard.Tests
{
    public class TemperatureFormatterTests
    {
        // ── ToFahrenheit ─────────────────────────────────────────────────────

        [Theory]
        [InlineData(0,    32.0)]    // freezing
        [InlineData(100,  212.0)]   // boiling
        [InlineData(-40,  -40.0)]   // crossover point
        [InlineData(20,   68.0)]    // comfortable room temp
        [InlineData(37,   98.6)]    // body temperature
        public void ToFahrenheit_KnownValues_ConvertsCorrectly(double celsius, double expectedF)
        {
            var result = TemperatureFormatter.ToFahrenheit(celsius);
            Assert.Equal(expectedF, result, precision: 1);
        }

        // ── ToCelsius ────────────────────────────────────────────────────────

        [Theory]
        [InlineData(32,   0.0)]
        [InlineData(212,  100.0)]
        [InlineData(-40,  -40.0)]
        public void ToCelsius_KnownValues_ConvertsCorrectly(double fahrenheit, double expectedC)
        {
            var result = TemperatureFormatter.ToCelsius(fahrenheit);
            Assert.Equal(expectedC, result, precision: 1);
        }

        // ── Format ───────────────────────────────────────────────────────────

        [Fact]
        public void Format_Celsius_ReturnsCelsiusString()
        {
            var result = TemperatureFormatter.Format(20.0, useCelsius: true);
            Assert.Equal("20.0\u00b0C", result);
        }

        [Fact]
        public void Format_Fahrenheit_ConvertsAndReturnsFahrenheitString()
        {
            // 0°C = 32°F
            var result = TemperatureFormatter.Format(0.0, useCelsius: false);
            Assert.Equal("32.0\u00b0F", result);
        }

        [Fact]
        public void Format_Fahrenheit_BodyTemp()
        {
            // 37°C = 98.6°F
            var result = TemperatureFormatter.Format(37.0, useCelsius: false);
            Assert.Equal("98.6\u00b0F", result);
        }

        [Fact]
        public void Format_NegativeCelsius_Fahrenheit_ConvertsCorrectly()
        {
            // -40°C = -40°F (the crossover)
            var result = TemperatureFormatter.Format(-40.0, useCelsius: false);
            Assert.Equal("-40.0\u00b0F", result);
        }

        // ── Round-trip ───────────────────────────────────────────────────────

        [Theory]
        [InlineData(0.0)]
        [InlineData(20.0)]
        [InlineData(-10.0)]
        [InlineData(37.0)]
        public void ToFahrenheit_ThenToCelsius_RoundTripsCorrectly(double original)
        {
            var fahrenheit = TemperatureFormatter.ToFahrenheit(original);
            var roundTrip  = TemperatureFormatter.ToCelsius(fahrenheit);
            Assert.Equal(original, roundTrip, precision: 1);
        }
    }
}
