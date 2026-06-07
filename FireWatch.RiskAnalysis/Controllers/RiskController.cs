namespace FireWatch.RiskAnalysis.Controllers;

[ApiController]
[Route("api/risk")]
[Produces("application/json")]
public class RiskController : ControllerBase
{
    private readonly IRiskService _service;

    public RiskController(IRiskService service) => _service = service;

    /// <summary>Analisa manualmente um registro espacial e retorna o score.</summary>
    [HttpPost("analyze")]
    [ProducesResponseType(typeof(RiskAssessmentResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Analyze(
        [FromBody] ManualRiskRequest request, CancellationToken ct)
    {
        var @event = new SpatialDataReceivedEvent(
            Guid.NewGuid(),
            request.Latitude, request.Longitude,
            request.Brightness, request.Frp, request.Confidence,
            request.Source, request.DayNight,
            request.AcquiredAt, DateTime.UtcNow
        );

        var result = await _service.AssessAsync(@event, ct);

        return Ok(new RiskAssessmentResponse(
            result.Id, result.SourceRecordId,
            result.Latitude, result.Longitude,
            result.Brightness, result.Frp, result.Confidence,
            result.Source, result.DayNight,
            result.RiskScore, result.RiskLevel.ToString(),
            result.RegionCode, result.AcquiredAt, result.CreatedAt
        ));
    }

    /// <summary>Lista assessments por região e período.</summary>
    [HttpGet("region/{regionCode}")]
    [ProducesResponseType(typeof(IReadOnlyList<RiskAssessmentResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByRegion(
        string regionCode,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        CancellationToken ct)
    {
        var results = await _service.GetByRegionAsync(regionCode, from, to, ct);
        return Ok(results);
    }

    /// <summary>Lista focos críticos das últimas 24h.</summary>
    [HttpGet("critical")]
    [ProducesResponseType(typeof(IReadOnlyList<RiskAssessmentResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCritical(CancellationToken ct)
    {
        var results = await _service.GetCriticalAsync(ct);
        return Ok(results);
    }

    /// <summary>Resumo de risco por região.</summary>
    [HttpGet("regions/summary")]
    [ProducesResponseType(typeof(IReadOnlyList<RegionSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRegionSummaries(CancellationToken ct)
    {
        var results = await _service.GetRegionSummariesAsync(ct);
        return Ok(results);
    }

    /// <summary>Busca assessment por ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(RiskAssessmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, ct);
        if (result is null)
            return NotFound(new { message = $"Assessment {id} não encontrado." });
        return Ok(result);
    }

    /// <summary>Health check.</summary>
    [HttpGet("health")]
    public IActionResult Health()
        => Ok(new { status = "healthy", service = "RiskAnalysis", timestamp = DateTime.UtcNow });
}