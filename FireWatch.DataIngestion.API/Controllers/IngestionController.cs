using FireWatch.DataIngestion.API.DTOs;
using FireWatch.DataIngestion.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace FireWatch.DataIngestion.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class IngestionController : ControllerBase
{
    private readonly IIngestionService _service;

    public IngestionController(IIngestionService service)
        => _service = service;

    /// <summary>
    /// Ingere um único registro espacial manualmente.
    /// Útil para testes ou ingestão de fontes customizadas.
    /// </summary>
    [HttpPost("single")]
    [ProducesResponseType(typeof(IngestionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> IngestSingle(
        [FromBody] SpatialDataRequest request,
        CancellationToken ct)
    {
        var result = await _service.IngestSingleAsync(new IngestSingleCommand(
            request.Latitude,
            request.Longitude,
            request.Brightness,
            request.Frp,
            request.Confidence,
            request.Source,
            request.ScanType,
            request.DayNight,
            request.AcquiredAt
        ), ct);

        return Ok(new IngestionResponse(
            result.Success, result.RecordsIngested, result.ErrorMessage));
    }

    /// <summary>
    /// Dispara ingestão em lote de uma fonte externa (ex: NasaFirms).
    /// Busca dados do período informado e publica no barramento.
    /// </summary>
    [HttpPost("bulk")]
    [ProducesResponseType(typeof(IngestionResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> IngestBulk(
        [FromBody] BulkIngestionRequest request,
        CancellationToken ct)
    {
        if (request.From >= request.To)
            return BadRequest(new { message = "From deve ser anterior a To." });

        var result = await _service.IngestFromSourceAsync(
            request.SourceName, request.From, request.To, ct);

        return Accepted(new IngestionResponse(
            result.Success, result.RecordsIngested, result.ErrorMessage));
    }

    /// <summary>
    /// Retorna registros persistidos filtrados por fonte e período.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<EspacialRecordDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecords(
        [FromQuery] string source,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        CancellationToken ct)
    {
        var records = await _service.GetBySourceAsync(source, from, to, ct);
        return Ok(records);
    }

    /// <summary>
    /// Retorna um registro específico pelo ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EspacialRecordDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var record = await _service.GetByIdAsync(id, ct);

        if (record is null)
            return NotFound(new { message = $"Registro {id} não encontrado." });

        return Ok(record);
    }

    /// <summary>Health check do serviço.</summary>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Health()
        => Ok(new { status = "healthy", service = "DataIngestion", timestamp = DateTime.UtcNow });
}