using FireWatch.DataIngestion.Application.Eventos;
using FireWatch.DataIngestion.Application.Interfaces;
using FireWatch.DataIngestion.Domain.Enums;
using FireWatch.DataIngestion.Domain.Exceptions;
using FireWatch.DataIngestion.Domain.Interfaces;
using FireWatch.DataIngestion.Domain.ValueObjects;
using FireWatch.DataIngestion.Domain.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FireWatch.DataIngestion.Application.Services;

public class IngestionService : IIngestionService
{
    protected readonly IEspacialRecordRepository _repository;
    protected readonly IEventPublisher _publisher;
    protected readonly IEnumerable<IDataSourceClient> _clients;
    protected readonly ILogger<IngestionService> _logger;

    public IngestionService(
        IEspacialRecordRepository repository,
        IEventPublisher publisher,
        IEnumerable<IDataSourceClient> clients,
        ILogger<IngestionService> logger)
    {
        _repository = repository;
        _publisher = publisher;
        _clients = clients;
        _logger = logger;
    }

    public async Task<IngestionResult> IngestSingleAsync(
        IngestSingleCommand cmd, CancellationToken ct = default)
    {
        try
        {
            var record = new EspacialRecord(
                new Coordinates(cmd.Latitude, cmd.Longitude),
                ParseSource(cmd.Source),
                cmd.Source,
                cmd.Brightness,
                cmd.Frp,
                cmd.Confidence,
                cmd.ScanType,
                cmd.DayNight,
                cmd.AcquiredAt
            );

            await _repository.AddAsync(record, ct);
            await _repository.SaveChangesAsync(ct);

            var @event = new EspacialDataReceivedEvento(
                record.Id,
                record.Coordinates.Latitude,
                record.Coordinates.Longitude,
                record.Brightness,
                record.Frp,
                record.Confidence,
                record.Source.ToString(),
                record.DayNight,
                record.AcquiredAt,
                DateTime.UtcNow
            );

            await _publisher.PublishAsync(@event, "firewatch.Espacial.received", ct);

            record.MarkAsPublished();
            await _repository.SaveChangesAsync(ct);

            return new IngestionResult(true, 1);
        }
        catch (InvalidCoordinatesException ex)
        {
            _logger.LogWarning("Coordenada inválida ignorada: {Msg}", ex.Message);
            return new IngestionResult(false, 0, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao ingerir registro.");
            return new IngestionResult(false, 0, "Erro interno ao processar registro.");
        }
    }

    public async Task<IngestionResult> IngestFromSourceAsync(
        string sourceName, DateTime from, DateTime to, CancellationToken ct = default)
        {
        var client = _clients.FirstOrDefault(c =>
            c.SourceName.Equals(sourceName, StringComparison.OrdinalIgnoreCase));

        if (client is null)
            return new IngestionResult(false, 0, $"Fonte '{sourceName}' não encontrada.");

        _logger.LogInformation("Iniciando ingestão de {Source} de {From} até {To}",
            sourceName, from, to);

        var rawList = await client.FetchAsync(from, to, ct);

        var records = new List<EspacialRecord>();

        foreach (var raw in rawList)
        {
            try
            {
                var acquiredAt = raw.AcquiredAt.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(raw.AcquiredAt, DateTimeKind.Utc)
            : raw.AcquiredAt.ToUniversalTime();

                records.Add(new EspacialRecord(
                    new Coordinates(raw.Latitude, raw.Longitude),
                    ParseSource(sourceName),
                    raw.SourceIdentifier,
                    raw.Brightness,
                    raw.Frp,
                    raw.Confidence,
                    raw.ScanType,
                    raw.DayNight,
                    acquiredAt
                ));
            }
            catch (InvalidCoordinatesException ex)
            {
                _logger.LogWarning("Ignorado: {Msg}", ex.Message);
            }
        }

        await _repository.AddRangeAsync(records, ct);
        await _repository.SaveChangesAsync(ct);

        foreach (var record in records)
        {
            try
            {
                var @event = new EspacialDataReceivedEvento(
                    record.Id,
                    record.Coordinates.Latitude,
                    record.Coordinates.Longitude,
                    record.Brightness,
                    record.Frp,
                    record.Confidence,
                    record.Source.ToString(),
                    record.DayNight,
                    record.AcquiredAt,
                    DateTime.UtcNow
                );

                await _publisher.PublishAsync(@event, "firewatch.Espacial.received", ct);
                record.MarkAsPublished();
            }
            catch (Exception ex)
            {
                record.MarkAsFailed(ex.Message);
                _logger.LogError(ex, "Falha ao publicar evento do record {Id}", record.Id);
            }
        }

        await _repository.SaveChangesAsync(ct);

        _logger.LogInformation("Ingestão concluída: {Count} registros de {Source}",
            records.Count, sourceName);

        return new IngestionResult(true, records.Count);
    }

    public async Task<IReadOnlyList<EspacialRecordDto>> GetBySourceAsync(
        string sourceName, DateTime from, DateTime to, CancellationToken ct = default)
    {
        var source = ParseSource(sourceName);
        var records = await _repository.GetBySourceAsync(source, from, to, ct);
        return records.Select(ToDto).ToList();
    }

    public async Task<EspacialRecordDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var record = await _repository.GetByIdAsync(id, ct);
        return record is null ? null : ToDto(record);
    }

    private static EspacialRecordDto ToDto(EspacialRecord r) => new(
        r.Id, r.Coordinates.Latitude, r.Coordinates.Longitude,
        r.Brightness, r.Frp, r.Confidence,
        r.Source.ToString(), r.DayNight, r.AcquiredAt,
        r.Status.ToString(), r.CreatedAt
    );

    private static DataSourceType ParseSource(string source) =>
        source.ToUpperInvariant() switch
        {
            "NASAFIRMS" or "NASA" or "NASA_FIRMS" => DataSourceType.NasaFirms,
            "INPE" => DataSourceType.Inpe,
            "OPENMETEO" => DataSourceType.OpenMeteo,
            "OPENAQ" => DataSourceType.OpenAQ,
            _ => DataSourceType.NasaFirms
        };
}