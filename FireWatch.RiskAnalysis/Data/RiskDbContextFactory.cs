using FireWatch.RiskAnalysis.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

public class RiskDbContextFactory : IDesignTimeDbContextFactory<RiskDbContext>
{
    public RiskDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<RiskDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=firewatch_risk;Username=postgres;Password=postgres")
            .Options;

        return new RiskDbContext(options);
    }
}