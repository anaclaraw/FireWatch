namespace FireWatch.Gateway.Middlewares;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (UnauthorizedAccessException ex)
        {
            await Write(context, 401, "UNAUTHORIZED", ex.Message);
        }
        catch (ArgumentNullException ex)
        {
            await Write(context, 400, "BAD_REQUEST", ex.Message); //"Operação não pode ser concluída, aramêtros nulos ou vazios."
        }
        catch (InvalidOperationException ex)
        {
            await Write(context, 400, "BAD_REQUEST", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro não tratado no Gateway");
            await Write(context, 500, "INTERNAL_ERROR", "Erro interno.");
        }
    }

    private static Task Write(HttpContext ctx, int status, string code, string msg)
    {
        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/json";
        return ctx.Response.WriteAsJsonAsync(new
        {
            success = false,
            errorCode = code,
            message = msg,
            timestamp = DateTime.UtcNow
        });
    }
}