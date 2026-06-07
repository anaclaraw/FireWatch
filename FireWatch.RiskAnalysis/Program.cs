using FireWatch.RiskAnalysis.Data;
using FireWatch.RiskAnalysis.Messaging;
using FireWatch.RiskAnalysis.Middlewares;
using FireWatch.RiskAnalysis.Services;
using FireWatch.RiskAnalysis.Services.Interfaces;
using FireWatch.RiskAnalysis.Validators;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Banco de dados
builder.Services.AddDbContext<RiskDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

// Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "FireWatch — Risk Analysis Service",
        Version = "v1",
        Description = "Calcula score de risco de queimadas a partir de dados espaciais."
    })
);

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<ManualRiskRequestValidator>();
builder.Services.AddFluentValidationAutoValidation();

// Services
builder.Services.AddScoped<IRiskService, RiskService>();

// Consumer RabbitMQ
builder.Services.AddHostedService<RabbitMQConsumer>();

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "FireWatch RiskAnalysis v1");
    c.RoutePrefix = string.Empty;
});

app.UseAuthorization();
app.MapControllers();

// Cria tabelas ao subir em dev
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<RiskDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.Run();