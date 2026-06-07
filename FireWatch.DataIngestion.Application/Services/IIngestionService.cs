using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FireWatch.DataIngestion.Application.Services
{
    public interface IIngestionService
    {
        Task<IngestionResult> IngestSingleAsync(
            IngestSingleCommand command, CancellationToken ct = default);

        Task<IngestionResult> IngestFromSourceAsync(
            string sourceName, DateTime from, DateTime to, CancellationToken ct = default);

        Task<IReadOnlyList<EspacialRecordDto>> GetBySourceAsync(
            string sourceName, DateTime from, DateTime to, CancellationToken ct = default);

        Task<EspacialRecordDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    }

    public record IngestSingleCommand(
        double Latitude,
        double Longitude,
        double Brightness,
        double Frp,
        double Confidence,
        string Source,
        string ScanType,
        string DayNight,
        DateTime AcquiredAt
    );

    public record IngestionResult(
        bool Success,
        int RecordsIngested,
        string? ErrorMessage = null
    );

    public record EspacialRecordDto(
        Guid Id,
        double Latitude,
        double Longitude,
        double Brightness,
        double Frp,
        double Confidence,
        string Source,
        string DayNight,
        DateTime AcquiredAt,
        string Status,
        DateTime CreatedAt
    );
}
