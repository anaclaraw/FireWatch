using FireWatch.RiskAnalysis.Data;
using FireWatch.RiskAnalysis.Models;
using FireWatch.RiskAnalysis.Services.Interfaces;

namespace FireWatch.RiskAnalysis.Services;

public class RiskService : IRiskService
{
    private readonly RiskDbContext _db;
    private readonly ILogger<RiskService> _logger;

    private const double BrightnessMin = 300.0;  
    private const double BrightnessMax = 420.0;  
    private const double FrpMin = 0.0;
    private const double FrpMax = 500.0;  

    private const double WeightBrightness = 0.30;
    private const double WeightFrp = 0.35;
    private const double WeightConfidence = 0.20;
    private const double WeightDensity = 0.15;

    public RiskService(RiskDbContext db, ILogger<RiskService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<RiskAssessment> AssessAsync(
        SpatialDataReceivedEvent @event, CancellationToken ct = default)
    {
        var regionCode = ResolveRegionCode(@event.Latitude, @event.Longitude);

        var brightScore = Normalize(@event.Brightness, BrightnessMin, BrightnessMax) * 100;

        var frpScore = Normalize(@event.Frp, FrpMin, FrpMax) * 100;

        var confScore = @event.Confidence;

        var recentCount = await _db.RiskAssessments
            .CountAsync(r => r.RegionCode == regionCode
                          && r.AcquiredAt >= @event.AcquiredAt.AddHours(-24), ct);

        var densityScore = Math.Min(recentCount / 50.0 * 100, 100);

        var finalScore = Math.Round(
            brightScore * WeightBrightness +
            frpScore * WeightFrp +
            confScore * WeightConfidence +
            densityScore * WeightDensity, 2);

        finalScore = Math.Clamp(finalScore, 0, 100);

        var assessment = new RiskAssessment
        {
            SourceRecordId = @event.RecordId,
            Latitude = @event.Latitude,
            Longitude = @event.Longitude,
            Brightness = @event.Brightness,
            Frp = @event.Frp,
            Confidence = @event.Confidence,
            Source = @event.Source,
            DayNight = @event.DayNight,
            AcquiredAt = @event.AcquiredAt,
            RiskScore = finalScore,
            RiskLevel = ClassifyRisk(finalScore),
            RegionCode = regionCode
        };

        _db.RiskAssessments.Add(assessment);
        await _db.SaveChangesAsync(ct);

        await UpdateRegionSummaryAsync(regionCode, ct);

        _logger.LogInformation(
            "Score calculado: {Score} ({Level}) | Região: {Region} | FRP: {Frp}MW",
            finalScore, assessment.RiskLevel, regionCode, @event.Frp);

        return assessment;
    }

    public async Task<IReadOnlyList<RiskAssessmentResponse>> GetByRegionAsync(
        string regionCode, DateTime from, DateTime to, CancellationToken ct = default)
    {
        var list = await _db.RiskAssessments
            .Where(r => r.RegionCode == regionCode
                     && r.AcquiredAt >= from
                     && r.AcquiredAt <= to)
            .OrderByDescending(r => r.RiskScore)
            .ToListAsync(ct);

        return list.Select(ToResponse).ToList();
    }

    public async Task<IReadOnlyList<RiskAssessmentResponse>> GetCriticalAsync(
        CancellationToken ct = default)
    {
        var since = DateTime.UtcNow.AddHours(-24);
        var list = await _db.RiskAssessments
            .Where(r => r.RiskLevel == RiskLevel.Critical
                     && r.AcquiredAt >= since)
            .OrderByDescending(r => r.RiskScore)
            .Take(100)
            .ToListAsync(ct);

        return list.Select(ToResponse).ToList();
    }

    public async Task<IReadOnlyList<RegionSummaryResponse>> GetRegionSummariesAsync(
        CancellationToken ct = default)
    {
        var list = await _db.RegionRiskSummaries
            .OrderByDescending(r => r.AverageRiskScore)
            .ToListAsync(ct);

        return list.Select(r => new RegionSummaryResponse(
            r.RegionCode,
            r.RegionName,
            r.AverageRiskScore,
            r.MaxRiskScore,
            r.TotalFocusCount,
            r.DominantLevel.ToString(),
            r.LastUpdatedAt
        )).ToList();
    }

    public async Task<RiskAssessmentResponse?> GetByIdAsync(
        Guid id, CancellationToken ct = default)
    {
        var r = await _db.RiskAssessments.FindAsync(new object[] { id }, ct);
        return r is null ? null : ToResponse(r);
    }

    private async Task UpdateRegionSummaryAsync(string regionCode, CancellationToken ct)
    {
        var since = DateTime.UtcNow.AddDays(-1);

        var assessments = await _db.RiskAssessments
            .Where(r => r.RegionCode == regionCode && r.AcquiredAt >= since)
            .ToListAsync(ct);

        if (!assessments.Any()) return;

        var summary = await _db.RegionRiskSummaries
            .FirstOrDefaultAsync(r => r.RegionCode == regionCode, ct);

        var avg = assessments.Average(r => r.RiskScore);
        var max = assessments.Max(r => r.RiskScore);

        if (summary is null)
        {
            summary = new RegionRiskSummary
            {
                RegionCode = regionCode,
                RegionName = ResolveRegionName(regionCode),
                PeriodStart = since,
                PeriodEnd = DateTime.UtcNow
            };
            _db.RegionRiskSummaries.Add(summary);
        }

        summary.AverageRiskScore = Math.Round(avg, 2);
        summary.MaxRiskScore = Math.Round(max, 2);
        summary.TotalFocusCount = assessments.Count;
        summary.DominantLevel = ClassifyRisk(avg);
        summary.LastUpdatedAt = DateTime.UtcNow;
        summary.PeriodEnd = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }

    private static double Normalize(double value, double min, double max)
        => Math.Clamp((value - min) / (max - min), 0.0, 1.0);

    private static RiskLevel ClassifyRisk(double score) => score switch
    {
        <= 25 => RiskLevel.Low,
        <= 50 => RiskLevel.Medium,
        <= 75 => RiskLevel.High,
        _ => RiskLevel.Critical
    };

    private static string ResolveRegionCode(double lat, double lon) => (lat, lon) switch
    {
        _ when lat >= -5 && lat <= 5 && lon >= -73 && lon <= -44 => "BR-AM",
        _ when lat >= -10 && lat < -5 && lon >= -73 && lon <= -44 => "BR-PA",
        _ when lat >= -15 && lat < -10 && lon >= -60 && lon <= -44 => "BR-MT",
        _ when lat >= -20 && lat < -15 && lon >= -60 && lon <= -40 => "BR-GO",
        _ when lat >= -25 && lat < -20 && lon >= -55 && lon <= -40 => "BR-MS",
        _ when lat >= -20 && lat < -14 && lon >= -48 && lon <= -38 => "BR-BA",
        _ when lat >= -30 && lat < -25 && lon >= -55 && lon <= -48 => "BR-PR",
        _ when lat >= -34 && lat < -30 && lon >= -54 && lon <= -49 => "BR-RS",
        _ => "BR-XX"
    };

    private static string ResolveRegionName(string code) => code switch
    {
        "BR-AM" => "Amazonas",
        "BR-PA" => "Pará",
        "BR-MT" => "Mato Grosso",
        "BR-GO" => "Goiás",
        "BR-MS" => "Mato Grosso do Sul",
        "BR-BA" => "Bahia",
        "BR-PR" => "Paraná",
        "BR-RS" => "Rio Grande do Sul",
        _ => "Região Desconhecida"
    };

    private static RiskAssessmentResponse ToResponse(RiskAssessment r) => new(
        r.Id, r.SourceRecordId,
        r.Latitude, r.Longitude,
        r.Brightness, r.Frp, r.Confidence,
        r.Source, r.DayNight,
        r.RiskScore, r.RiskLevel.ToString(),
        r.RegionCode, r.AcquiredAt, r.CreatedAt
    );
}