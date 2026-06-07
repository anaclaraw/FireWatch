namespace FireWatch.Gateway.Extensions;

public static class CorsExtensions
{
    public static IServiceCollection AddFireWatchCors(
        this IServiceCollection services, IConfiguration config)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("FireWatchPolicy", policy =>
            {
                
                policy
                    .WithOrigins(
                        "http://localhost:8081" // app mobile expo
                    )
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        return services;
    }
}