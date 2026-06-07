namespace FireWatch.RiskAnalysis.Models;

public class RegionRiskSummary
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string RegionCode { get; set; } = string.Empty;
    public string RegionName { get; set; } = string.Empty;
    public double AverageRiskScore { get; set; }
    public double MaxRiskScore { get; set; }
    public int TotalFocusCount { get; set; }
    public RiskLevel DominantLevel { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
}