namespace FireWatch.DataIngestion.API.DTOs;

public record SpatialDataRequest(
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

public record BulkIngestionRequest(
    string SourceName,
    DateTime From,
    DateTime To
);

public record IngestionResponse(
    bool Success,
    int RecordsIngested,
    string? ErrorMessage
);