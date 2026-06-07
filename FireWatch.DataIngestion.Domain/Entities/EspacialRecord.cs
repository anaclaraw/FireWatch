using FireWatch.DataIngestion.Domain.Enums;
using FireWatch.DataIngestion.Domain.ValueObjects;


namespace FireWatch.DataIngestion.Domain.Entities;

public class EspacialRecord : BaseEntity
{
    public Coordinates Coordinates { get; private set; } = null!;
    public DataSourceType Source { get; private set; }
    public string SourceIdentifier { get; private set; } = string.Empty;

    
    public double Brightness { get; private set; }   // kelvin
    public double Frp { get; private set; }  
    public double Confidence { get; private set; }   
    public string ScanType { get; private set; } = string.Empty;
    public string DayNight { get; private set; } = string.Empty;

    public DateTime AcquiredAt { get; private set; }
    public ProcessingStatus Status { get; private set; }
    public string? FailureReason { get; private set; }

    protected EspacialRecord() { } 

    public EspacialRecord(
        Coordinates coordinates,
        DataSourceType source,
        string sourceIdentifier,
        double brightness,
        double frp,
        double confidence,
        string scanType,
        string dayNight,
        DateTime acquiredAt)
    {
        Coordinates = coordinates;
        Source = source;
        SourceIdentifier = sourceIdentifier;
        Brightness = brightness;
        Frp = Math.Max(0, frp);
        Confidence = Math.Clamp(confidence, 0, 100);
        ScanType = scanType;
        DayNight = dayNight;
        AcquiredAt = acquiredAt;
        Status = ProcessingStatus.Pending;
    }

    public void MarkAsPublished()
    {
        Status = ProcessingStatus.Published;
        SetUpdated();
    }

    public void MarkAsFailed(string reason)
    {
        Status = ProcessingStatus.Failed;
        FailureReason = reason;
        SetUpdated();
    }
}