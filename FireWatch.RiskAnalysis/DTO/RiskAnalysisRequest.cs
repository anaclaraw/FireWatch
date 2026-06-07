namespace FireWatch.RiskAnalysis.DTOs;

public record ManualRiskRequest(
    double Latitude,
    double Longitude,
    double Brightness,
    double Frp,
    double Confidence,
    string Source,
    string DayNight,
    DateTime AcquiredAt
);

public record RiskAssessmentResponse(
    Guid Id,
    Guid SourceRecordId,
    double Latitude,
    double Longitude,
    double Brightness,
    double Frp,
    double Confidence,
    string Source,
    string DayNight,
    double RiskScore,
    string RiskLevel,
    string RegionCode,
    DateTime AcquiredAt,
    DateTime CreatedAt
);

public record RegionSummaryResponse(
    string RegionCode,
    string RegionName,
    double AverageRiskScore,
    double MaxRiskScore,
    int TotalFocusCount,
    string DominantLevel,
    DateTime LastUpdatedAt
);