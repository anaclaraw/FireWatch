using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace FireWatch.Gateway.Extensions;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddFireWatchRateLimiting(
        this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.AddSlidingWindowLimiter("default", opt =>
            {
                opt.PermitLimit = 30;
                opt.Window = TimeSpan.FromMinutes(1);
                opt.SegmentsPerWindow = 6;
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 5;
            });

            options.AddSlidingWindowLimiter("auth", opt =>
            {
                opt.PermitLimit = 10;
                opt.Window = TimeSpan.FromMinutes(1);
                opt.SegmentsPerWindow = 6;
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 2;
            });

            options.OnRejected = async (context, ct) =>
            {
                context.HttpContext.Response.StatusCode = 429;
                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    success = false,
                    errorCode = "RATE_LIMIT_EXCEEDED",
                    message = "Muitas requisições. Tente novamente em alguns instantes.",
                    timestamp = DateTime.UtcNow
                }, ct);
            };
        });

        return services;
    }
}