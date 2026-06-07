namespace FireWatch.RiskAnalysis.Models;

public class RiskAssessment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid SourceRecordId { get; set; }  
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Brightness { get; set; }
    public double Frp { get; set; }
    public double Confidence { get; set; }
    public string Source { get; set; } = string.Empty;
    public string DayNight { get; set; } = string.Empty;
    public DateTime AcquiredAt { get; set; }

    // Score calculado pelo algoritmo
    public double RiskScore { get; set; }        // 0–100
    public RiskLevel RiskLevel { get; set; }
    public string RegionCode { get; set; } = string.Empty; // ex: "BR-MT", "BR-PA"

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum RiskLevel
{
    Low = 1,   // 0–25
    Medium = 2,   // 26–50
    High = 3,   // 51–75
    Critical = 4    // 76–100
}