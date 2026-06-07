global using System.Reflection;
global using System.Text;
global using System.Text.Json;
global using FireWatch.DataIngestion.Application.DTOs;
global using FireWatch.DataIngestion.Application.Eventos;
global using FireWatch.DataIngestion.Application.Interfaces;
global using FireWatch.DataIngestion.Domain.Entities;
global using FireWatch.DataIngestion.Domain.Enums;
global using FireWatch.DataIngestion.Domain.Exceptions;
global using FireWatch.DataIngestion.Domain.Interfaces;
global using FireWatch.DataIngestion.Domain.ValueObjects;
global using FireWatch.DataIngestion.Infrastructure.Messaging;
global using FireWatch.DataIngestion.Infrastructure.Persistence;
global using FireWatch.DataIngestion.Infrastructure.Clients;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.EntityFrameworkCore.Metadata.Builders;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;
global using RabbitMQ.Client;
using FireWatch.DataIngestion.Infrastructure.Repositories;
using FireWatch.DataIngestion.Application.Services;
namespace FireWatch.DataIngestion.Infrastructure.DI;


public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(opt =>
            opt.UseNpgsql(
                config.GetConnectionString("Postgres"),
                npgsql => npgsql
                    .MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)
                    .EnableRetryOnFailure(3)
            )
        );

        // repository
        services.AddScoped<IEspacialRecordRepository, EspacialRecordRepository>();

        // rabbitMQ
        services.AddSingleton<IEventPublisher, RabbitMQEventPublisher>();

        services.AddHttpClient<IDataSourceClient, FirmsHttpClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }

    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddScoped<IIngestionService, IngestionService>();

       
       

        return services;
    }
}
