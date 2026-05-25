using OrderService.Api;
using OrderService.Api.Middleware;
using OrderService.Application;
using OrderService.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// ── Services ──────────────────────────────────────────────────────────────────

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerWithJwt();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<OrderService.Infrastructure.Persistence.AppDbContext>("database");

builder.Services.AddAuthorization();

// ── Pipeline ──────────────────────────────────────────────────────────────────

var app = builder.Build();

// Inicializa banco: migrations + seed (antes de aceitar requests)
await app.InitializeDatabaseAsync();

app.UseMiddleware<GlobalExceptionMiddleware>();

//if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Docker"))
//{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "OrderService API v1");
        c.RoutePrefix = string.Empty; // Swagger na raiz: http://localhost:8080
    });
//}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health");

app.Run();

// Necessário para WebApplicationFactory nos integration tests
public partial class Program { }
