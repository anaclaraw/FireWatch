using FireWatch.Gateway.Data;
using FireWatch.Gateway.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FireWatch.Gateway.Data;

public class GatewayDbContextFactory : IDesignTimeDbContextFactory<GatewayDbContext>
{
    public GatewayDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<GatewayDbContext>();

        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=firewatch_gateway;Username=postgres;Password=postgres"
        );

        return new GatewayDbContext(optionsBuilder.Options);
    }
}