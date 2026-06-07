using FireWatch.DataIngestion.Domain.Exceptions;

namespace FireWatch.DataIngestion.API.Middlewares;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
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
        catch (InvalidCoordinatesException ex)
        {
            await WriteResponse(context, 400, "INVALID_COORDINATES", ex.Message);
        }
        catch (NotFoundException ex)
        {
            await WriteResponse(context, 404, "NOT_FOUND", ex.Message);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning("Erro de domínio: {Msg}", ex.Message);
            await WriteResponse(context, 400, "DOMAIN_ERROR", ex.Message);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning("Erro ao interagir com banco de dados: {Msg}", ex.Message);
            await WriteResponse(context, 500, "DATABASE_ERROR", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro não tratado em {Path}", context.Request.Path);
            await WriteResponse(context, 500, "INTERNAL_ERROR",
                "Erro interno no servidor. Tente novamente.");
        }
    }

    private static Task WriteResponse(
        HttpContext ctx, int status, string code, string message)
    {
        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/json";

        return ctx.Response.WriteAsJsonAsync(new
        {
            success = false,
            errorCode = code,
            message,
            timestamp = DateTime.UtcNow
        });
    }
}
