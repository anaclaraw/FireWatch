using FireWatch.Gateway.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

public class DesignTimeFactory : IDesignTimeDbContextFactory<GatewayDbContext>
{
    public GatewayDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<GatewayDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=firewatch_gateway;Username=postgres;Password=postgres")
            .Options;

        return new GatewayDbContext(options);
    }
}
