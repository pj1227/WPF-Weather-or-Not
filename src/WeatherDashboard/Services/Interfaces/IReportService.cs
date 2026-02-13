using System;
using System.Threading.Tasks;

namespace WeatherDashboard.Services.Interfaces
{
    public interface IReportService
    {
        Task<byte[]> GeneratePdfReportAsync(int locationId, DateTime from, DateTime to);
        Task<byte[]> GenerateExcelReportAsync(int locationId, DateTime from, DateTime to);
    }
}