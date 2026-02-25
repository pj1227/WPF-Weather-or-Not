using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Moq;
using OfficeOpenXml;
using WeatherDashboard.Data.Entities;
using WeatherDashboard.Services;
using WeatherDashboard.Services.Interfaces;
using WeatherDashboard.Tests.TestHelpers;
using Xunit;

namespace WeatherDashboard.Tests
{
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

        private static ExcelWorksheet OpenWorksheet(byte[] bytes)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            // Note: caller owns the package lifetime — in tests we accept the leak
            // for brevity. Use 'using var pkg' in production-quality test helpers.
            var pkg = new ExcelPackage(new MemoryStream(bytes));
            return pkg.Workbook.Worksheets["Weather Data"];
        }

        // ── Excel — temperature values ────────────────────────────────────────

        [Fact]
        public async Task GenerateExcelReport_Celsius_TemperatureColumnIsRawValue()
        {
            var service = CreateService(true, out _, out var mockData);
            mockData.Setup(d => d.GetLocationByIdAsync(1)).ReturnsAsync(SampleLocation());
            mockData.Setup(d => d.GetWeatherHistoryAsync(1, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                    .ReturnsAsync(SampleRecords());

            var bytes = await service.GenerateExcelReportAsync(1, DateTime.Now.AddDays(-7), DateTime.Now);
            var ws    = OpenWorksheet(bytes);

            // Records are ordered ascending by Timestamp, so row 12 = oldest = 0°C
            var temp = Convert.ToDouble(ws.Cells[12, 2].Value);
            Assert.Equal(0.0, temp, precision: 1);
        }

        [Fact]
        public async Task GenerateExcelReport_Fahrenheit_TemperatureColumnIsConverted()
        {
            var service = CreateService(false, out _, out var mockData);
            mockData.Setup(d => d.GetLocationByIdAsync(1)).ReturnsAsync(SampleLocation());
            mockData.Setup(d => d.GetWeatherHistoryAsync(1, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                    .ReturnsAsync(SampleRecords());

            var bytes = await service.GenerateExcelReportAsync(1, DateTime.Now.AddDays(-7), DateTime.Now);
            var ws    = OpenWorksheet(bytes);

            // 0°C = 32°F
            var temp = Convert.ToDouble(ws.Cells[12, 2].Value);
            Assert.Equal(32.0, temp, precision: 1);
        }

        [Fact]
        public async Task GenerateExcelReport_Fahrenheit_FeelsLikeColumnIsConverted()
        {
            var service = CreateService(false, out _, out var mockData);
            mockData.Setup(d => d.GetLocationByIdAsync(1)).ReturnsAsync(SampleLocation());
            mockData.Setup(d => d.GetWeatherHistoryAsync(1, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                    .ReturnsAsync(SampleRecords());

            var bytes = await service.GenerateExcelReportAsync(1, DateTime.Now.AddDays(-7), DateTime.Now);
            var ws    = OpenWorksheet(bytes);

            // -5°C = 23°F
            var feelsLike = Convert.ToDouble(ws.Cells[12, 3].Value);
            Assert.Equal(23.0, feelsLike, precision: 1);
        }

        // ── Excel — column headers ────────────────────────────────────────────

        [Fact]
        public async Task GenerateExcelReport_Celsius_TemperatureHeaderContainsCelsius()
        {
            var service = CreateService(true, out _, out var mockData);
            mockData.Setup(d => d.GetLocationByIdAsync(1)).ReturnsAsync(SampleLocation());
            mockData.Setup(d => d.GetWeatherHistoryAsync(1, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                    .ReturnsAsync(SampleRecords());

            var bytes  = await service.GenerateExcelReportAsync(1, DateTime.Now.AddDays(-7), DateTime.Now);
            var ws     = OpenWorksheet(bytes);
            var header = ws.Cells[11, 2].Value?.ToString();

            Assert.Contains("\u00b0C", header);
        }

        [Fact]
        public async Task GenerateExcelReport_Fahrenheit_TemperatureHeaderContainsFahrenheit()
        {
            var service = CreateService(false, out _, out var mockData);
            mockData.Setup(d => d.GetLocationByIdAsync(1)).ReturnsAsync(SampleLocation());
            mockData.Setup(d => d.GetWeatherHistoryAsync(1, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                    .ReturnsAsync(SampleRecords());

            var bytes  = await service.GenerateExcelReportAsync(1, DateTime.Now.AddDays(-7), DateTime.Now);
            var ws     = OpenWorksheet(bytes);
            var header = ws.Cells[11, 2].Value?.ToString();

            Assert.Contains("\u00b0F", header);
        }

        // ── Excel — statistics ────────────────────────────────────────────────

        [Fact]
        public async Task GenerateExcelReport_Celsius_StatAvgIsRawCelsius()
        {
            var service = CreateService(true, out _, out var mockData);
            mockData.Setup(d => d.GetLocationByIdAsync(1)).ReturnsAsync(SampleLocation());
            mockData.Setup(d => d.GetWeatherHistoryAsync(1, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                    .ReturnsAsync(SampleRecords());

            var bytes = await service.GenerateExcelReportAsync(1, DateTime.Now.AddDays(-7), DateTime.Now);
            var ws    = OpenWorksheet(bytes);

            // avg of (0°C, 20°C) = 10°C
            var avg = Convert.ToDouble(ws.Cells[6, 2].Value);
            Assert.Equal(10.0, avg, precision: 1);
        }

        [Fact]
        public async Task GenerateExcelReport_Fahrenheit_StatAvgIsConverted()
        {
            var service = CreateService(false, out _, out var mockData);
            mockData.Setup(d => d.GetLocationByIdAsync(1)).ReturnsAsync(SampleLocation());
            mockData.Setup(d => d.GetWeatherHistoryAsync(1, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                    .ReturnsAsync(SampleRecords());

            var bytes = await service.GenerateExcelReportAsync(1, DateTime.Now.AddDays(-7), DateTime.Now);
            var ws    = OpenWorksheet(bytes);

            // avg of (0°C, 20°C) = 10°C = 50°F
            var avg = Convert.ToDouble(ws.Cells[6, 2].Value);
            Assert.Equal(50.0, avg, precision: 1);
        }

        [Fact]
        public async Task GenerateExcelReport_Fahrenheit_StatMaxIsConverted()
        {
            var service = CreateService(false, out _, out var mockData);
            mockData.Setup(d => d.GetLocationByIdAsync(1)).ReturnsAsync(SampleLocation());
            mockData.Setup(d => d.GetWeatherHistoryAsync(1, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                    .ReturnsAsync(SampleRecords());

            var bytes = await service.GenerateExcelReportAsync(1, DateTime.Now.AddDays(-7), DateTime.Now);
            var ws    = OpenWorksheet(bytes);

            // max = 20°C = 68°F
            var max = Convert.ToDouble(ws.Cells[7, 2].Value);
            Assert.Equal(68.0, max, precision: 1);
        }

        [Fact]
        public async Task GenerateExcelReport_Fahrenheit_StatMinIsConverted()
        {
            var service = CreateService(false, out _, out var mockData);
            mockData.Setup(d => d.GetLocationByIdAsync(1)).ReturnsAsync(SampleLocation());
            mockData.Setup(d => d.GetWeatherHistoryAsync(1, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                    .ReturnsAsync(SampleRecords());

            var bytes = await service.GenerateExcelReportAsync(1, DateTime.Now.AddDays(-7), DateTime.Now);
            var ws    = OpenWorksheet(bytes);

            // min = 0°C = 32°F
            var min = Convert.ToDouble(ws.Cells[8, 2].Value);
            Assert.Equal(32.0, min, precision: 1);
        }

        // ── PDF — smoke tests ─────────────────────────────────────────────────

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

        // ── Error guard ───────────────────────────────────────────────────────

        [Fact]
        public async Task GeneratePdfReport_NoRecords_ThrowsException()
        {
            var service = CreateService(true, out _, out var mockData);
            mockData.Setup(d => d.GetLocationByIdAsync(1)).ReturnsAsync(SampleLocation());
            mockData.Setup(d => d.GetWeatherHistoryAsync(1, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                    .ReturnsAsync(new List<WeatherRecord>());

            await Assert.ThrowsAsync<Exception>(() =>
                service.GeneratePdfReportAsync(1, DateTime.Now.AddDays(-7), DateTime.Now));
        }

        [Fact]
        public async Task GenerateExcelReport_NoRecords_ThrowsException()
        {
            var service = CreateService(true, out _, out var mockData);
            mockData.Setup(d => d.GetLocationByIdAsync(1)).ReturnsAsync(SampleLocation());
            mockData.Setup(d => d.GetWeatherHistoryAsync(1, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                    .ReturnsAsync(new List<WeatherRecord>());

            await Assert.ThrowsAsync<Exception>(() =>
                service.GenerateExcelReportAsync(1, DateTime.Now.AddDays(-7), DateTime.Now));
        }

        [Fact]
        public async Task GeneratePdfReport_NullLocation_ThrowsException()
        {
            var service = CreateService(true, out _, out var mockData);
            mockData.Setup(d => d.GetLocationByIdAsync(1)).ReturnsAsync((SavedLocation?)null);
            mockData.Setup(d => d.GetWeatherHistoryAsync(1, It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                    .ReturnsAsync(SampleRecords());

            await Assert.ThrowsAsync<Exception>(() =>
                service.GeneratePdfReportAsync(1, DateTime.Now.AddDays(-7), DateTime.Now));
        }
    }
}
