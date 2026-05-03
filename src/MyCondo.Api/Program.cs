using MyCondo.Api;
using MyCondo.Api.HealthChecks;
using MyCondo.Api.Middleware;
using MyCondo.Application;
using MyCondo.Infrastructure;
using Scalar.AspNetCore;
using Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, config) =>
    config.ReadFrom.Configuration(context.Configuration)
          .ReadFrom.Services(services)
          .Enrich.FromLogContext()
          .Enrich.WithCorrelationId());

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddApiServices(builder.Configuration);

WebApplication app = builder.Build();

app.UseSerilogRequestLogging();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("DefaultCors");
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapMyCondoHealthChecks();

app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options
        .WithTitle("MyCondo API")
        .WithTheme(ScalarTheme.BluePlanet);
});

app.MapGet("/", () => Results.Redirect("/scalar"));

await app.RunAsync();

public partial class Program { }
