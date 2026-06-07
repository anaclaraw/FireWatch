using FireWatch.DataIngestion.API.Middlewares;
using FireWatch.DataIngestion.API.Validators;
using FireWatch.DataIngestion.Infrastructure.DI;
using FireWatch.DataIngestion.Infrastructure.Persistence;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<SpatialDataRequestValidator>();
builder.Services.AddFluentValidationAutoValidation();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "FireWatch — Data Ingestion Service",
        Version = "v1",
        Description =
            "Serviço responsável por coletar, normalizar e publicar " +
            "dados espaciais de focos de calor no barramento de eventos."
    });

    var xmlFile =
        $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";

    var xmlPath =
        Path.Combine(AppContext.BaseDirectory, xmlFile);

    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath);
});

// Application + Infrastructure
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Middleware global de exceção
app.UseMiddleware<GlobalExceptionMiddleware>();

// Swagger
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint(
        "/swagger/v1/swagger.json",
        "FireWatch DataIngestion v1");

    c.RoutePrefix = string.Empty;
});

// Authorization
app.UseAuthorization();

// Controllers
app.MapControllers();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();

    var db =
        scope.ServiceProvider.GetRequiredService<AppDbContext>();

    await db.Database.MigrateAsync();
}

app.Run();