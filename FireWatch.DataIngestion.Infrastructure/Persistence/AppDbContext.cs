using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using FireWatch.DataIngestion.Domain.Entities;
using Microsoft.EntityFrameworkCore;


namespace FireWatch.DataIngestion.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<EspacialRecord> EspacialRecords => Set<EspacialRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }
}