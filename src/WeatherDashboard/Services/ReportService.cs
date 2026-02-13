using OfficeOpenXml;
using OfficeOpenXml.Style;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using WeatherDashboard.Services.Interfaces;

namespace WeatherDashboard.Services
{
    public class ReportService : IReportService
    {
        private readonly IDataService _dataService;

        public ReportService(IDataService dataService)
        {
            _dataService = dataService;

            // Set QuestPDF license for non-commercial use
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public async Task<byte[]> GeneratePdfReportAsync(int locationId, DateTime from, DateTime to)
        {
            var location = await _dataService.GetLocationByIdAsync(locationId);
            var records = await _dataService.GetWeatherHistoryAsync(locationId, from, to);

            if (location == null || !records.Any())
            {
                throw new Exception("No data available for the selected period.");
            }

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header()
                        .Text($"Weather Report - {location.Name}")
                        .FontSize(20)
                        .Bold()
                        .FontColor(Colors.Blue.Darken2);

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(column =>
                        {
                            column.Spacing(10);

                            // Report Info
                            column.Item().Text($"Period: {from:MMM d, yyyy} - {to:MMM d, yyyy}");
                            column.Item().Text($"Location: {location.Name}, {location.Country}");
                            column.Item().Text($"Generated: {DateTime.Now:g}");

                            column.Item().PaddingTop(20);

                            // Statistics
                            var avgTemp = records.Average(r => r.Temperature);
                            var maxTemp = records.Max(r => r.Temperature);
                            var minTemp = records.Min(r => r.Temperature);
                            var avgHumidity = records.Average(r => r.Humidity);

                            column.Item().Text("Summary Statistics").FontSize(16).Bold();
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(150);
                                    columns.RelativeColumn();
                                });

                                table.Cell().Element(CellStyle).Text("Average Temperature:");
                                table.Cell().Element(CellStyle).Text($"{avgTemp:F1}°C");

                                table.Cell().Element(CellStyle).Text("Maximum Temperature:");
                                table.Cell().Element(CellStyle).Text($"{maxTemp:F1}°C");

                                table.Cell().Element(CellStyle).Text("Minimum Temperature:");
                                table.Cell().Element(CellStyle).Text($"{minTemp:F1}°C");

                                table.Cell().Element(CellStyle).Text("Average Humidity:");
                                table.Cell().Element(CellStyle).Text($"{avgHumidity:F1}%");
                            });

                            column.Item().PaddingTop(20);

                            // Detailed Records
                            column.Item().Text("Detailed Records").FontSize(16).Bold();
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn(2);
                                });

                                // Header
                                table.Header(header =>
                                {
                                    header.Cell().Element(HeaderStyle).Text("Date/Time");
                                    header.Cell().Element(HeaderStyle).Text("Temp");
                                    header.Cell().Element(HeaderStyle).Text("Humidity");
                                    header.Cell().Element(HeaderStyle).Text("Wind");
                                    header.Cell().Element(HeaderStyle).Text("Conditions");
                                });

                                // Data rows (limit to first 50 for PDF size)
                                foreach (var record in records.OrderByDescending(r => r.Timestamp).Take(50))
                                {
                                    table.Cell().Element(CellStyle).Text(record.Timestamp.ToString("MMM d, h:mm tt"));
                                    table.Cell().Element(CellStyle).Text($"{record.Temperature:F1}°C");
                                    table.Cell().Element(CellStyle).Text($"{record.Humidity:F0}%");
                                    table.Cell().Element(CellStyle).Text($"{record.WindSpeed:F1} m/s");
                                    table.Cell().Element(CellStyle).Text(record.Description);
                                }
                            });

                            if (records.Count > 50)
                            {
                                column.Item().PaddingTop(10).Text($"Note: Showing most recent 50 of {records.Count} records")
                                    .FontSize(10).Italic().FontColor(Colors.Grey.Darken1);
                            }
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });
                });
            });

            return document.GeneratePdf();
        }

        public async Task<byte[]> GenerateExcelReportAsync(int locationId, DateTime from, DateTime to)
        {
            var location = await _dataService.GetLocationByIdAsync(locationId);
            var records = await _dataService.GetWeatherHistoryAsync(locationId, from, to);

            if (location == null || !records.Any())
            {
                throw new Exception("No data available for the selected period.");
            }

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Weather Data");

            // Header
            worksheet.Cells[1, 1].Value = "Weather Report";
            worksheet.Cells[1, 1].Style.Font.Size = 16;
            worksheet.Cells[1, 1].Style.Font.Bold = true;

            worksheet.Cells[2, 1].Value = $"Location: {location.Name}, {location.Country}";
            worksheet.Cells[3, 1].Value = $"Period: {from:MMM d, yyyy} - {to:MMM d, yyyy}";

            // Statistics
            worksheet.Cells[5, 1].Value = "Summary Statistics";
            worksheet.Cells[5, 1].Style.Font.Bold = true;

            worksheet.Cells[6, 1].Value = "Average Temperature:";
            worksheet.Cells[6, 2].Value = records.Average(r => r.Temperature);
            worksheet.Cells[6, 2].Style.Numberformat.Format = "0.0\"°C\"";

            worksheet.Cells[7, 1].Value = "Maximum Temperature:";
            worksheet.Cells[7, 2].Value = records.Max(r => r.Temperature);
            worksheet.Cells[7, 2].Style.Numberformat.Format = "0.0\"°C\"";

            worksheet.Cells[8, 1].Value = "Minimum Temperature:";
            worksheet.Cells[8, 2].Value = records.Min(r => r.Temperature);
            worksheet.Cells[8, 2].Style.Numberformat.Format = "0.0\"°C\"";

            worksheet.Cells[9, 1].Value = "Average Humidity:";
            worksheet.Cells[9, 2].Value = records.Average(r => r.Humidity);
            worksheet.Cells[9, 2].Style.Numberformat.Format = "0.0\"%\"";

            // Column headers for detailed data
            var headerRow = 11;
            worksheet.Cells[headerRow, 1].Value = "Date/Time";
            worksheet.Cells[headerRow, 2].Value = "Temperature (°C)";
            worksheet.Cells[headerRow, 3].Value = "Feels Like (°C)";
            worksheet.Cells[headerRow, 4].Value = "Humidity (%)";
            worksheet.Cells[headerRow, 5].Value = "Pressure (hPa)";
            worksheet.Cells[headerRow, 6].Value = "Wind Speed (m/s)";
            worksheet.Cells[headerRow, 7].Value = "Description";

            using (var range = worksheet.Cells[headerRow, 1, headerRow, 7])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
            }

            // Data rows
            var row = headerRow + 1;
            foreach (var record in records.OrderBy(r => r.Timestamp))
            {
                worksheet.Cells[row, 1].Value = record.Timestamp.ToString("MMM d, yyyy h:mm tt");
                worksheet.Cells[row, 2].Value = record.Temperature;
                worksheet.Cells[row, 3].Value = record.FeelsLike;
                worksheet.Cells[row, 4].Value = record.Humidity;
                worksheet.Cells[row, 5].Value = record.Pressure;
                worksheet.Cells[row, 6].Value = record.WindSpeed;
                worksheet.Cells[row, 7].Value = record.Description;
                row++;
            }

            // Format
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            worksheet.Cells[headerRow + 1, 2, row - 1, 6].Style.Numberformat.Format = "0.00";

            return package.GetAsByteArray();
        }

        private static IContainer CellStyle(IContainer container)
        {
            return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5);
        }

        private static IContainer HeaderStyle(IContainer container)
        {
            return container.BorderBottom(2).BorderColor(Colors.Blue.Medium).Padding(5).Background(Colors.Grey.Lighten3);
        }
    }
}