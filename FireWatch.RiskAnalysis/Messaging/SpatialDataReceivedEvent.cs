namespace FireWatch.RiskAnalysis.Messaging;

public record SpatialDataReceivedEvent(
    Guid RecordId,
    double Latitude,
    double Longitude,
    double Brightness,
    double Frp,
    double Confidence,
    string Source,
    string DayNight,
    DateTime AcquiredAt,
    DateTime PublishedAt
);