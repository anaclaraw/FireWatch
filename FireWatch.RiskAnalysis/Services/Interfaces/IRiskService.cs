using FireWatch.RiskAnalysis.Models;

namespace FireWatch.RiskAnalysis.Services.Interfaces;

public interface IRiskService
{
   
    Task<RiskAssessment> AssessAsync(
        SpatialDataReceivedEvent @event, CancellationToken ct = default);

    Task<IReadOnlyList<RiskAssessmentResponse>> GetByRegionAsync(
        string regionCode, DateTime from, DateTime to, CancellationToken ct = default);

    Task<IReadOnlyList<RiskAssessmentResponse>> GetCriticalAsync(
        CancellationToken ct = default);

    Task<IReadOnlyList<RegionSummaryResponse>> GetRegionSummariesAsync(
        CancellationToken ct = default);

    Task<RiskAssessmentResponse?> GetByIdAsync(
        Guid id, CancellationToken ct = default);
}