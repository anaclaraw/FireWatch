using FireWatch.RiskAnalysis.Models;
using Microsoft.EntityFrameworkCore;

namespace FireWatch.RiskAnalysis.Data;

public class RiskDbContext : DbContext
{
    public RiskDbContext(DbContextOptions<RiskDbContext> options) : base(options) { }

    public DbSet<RiskAssessment> RiskAssessments => Set<RiskAssessment>();
    public DbSet<RegionRiskSummary> RegionRiskSummaries => Set<RegionRiskSummary>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RiskAssessment>(e =>
        {
            e.ToTable("risk_assessments");
            e.HasKey(x => x.Id);
            e.Property(x => x.RiskScore).HasPrecision(5, 2);
            e.Property(x => x.Latitude).HasPrecision(10, 7);
            e.Property(x => x.Longitude).HasPrecision(10, 7);
            e.Property(x => x.Brightness).HasPrecision(10, 4);
            e.Property(x => x.Frp).HasPrecision(10, 4);
            e.Property(x => x.Confidence).HasPrecision(5, 2);
            e.Property(x => x.Source).HasMaxLength(50);
            e.Property(x => x.DayNight).HasMaxLength(1);
            e.Property(x => x.RegionCode).HasMaxLength(10);
            e.Property(x => x.RiskLevel).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(x => x.RegionCode);
            e.HasIndex(x => x.AcquiredAt);
            e.HasIndex(x => x.RiskLevel);
        });

        modelBuilder.Entity<RegionRiskSummary>(e =>
        {
            e.ToTable("region_risk_summaries");
            e.HasKey(x => x.Id);
            e.Property(x => x.RegionCode).HasMaxLength(10).IsRequired();
            e.Property(x => x.RegionName).HasMaxLength(100);
            e.Property(x => x.AverageRiskScore).HasPrecision(5, 2);
            e.Property(x => x.MaxRiskScore).HasPrecision(5, 2);
            e.Property(x => x.DominantLevel).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(x => x.RegionCode);
        });
    }
}