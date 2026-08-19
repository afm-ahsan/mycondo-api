using Microsoft.Extensions.Hosting;
using MyCondo.Api;
using MyCondo.Api.Endpoints;
using MyCondo.Api.HealthChecks;
using MyCondo.Api.Middleware;
using MyCondo.Application;
using MyCondo.Infrastructure;
using MyCondo.Infrastructure.Seed;
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

// One explicit, auditable seed-orchestration entry point — see DatabaseSeederExtensions for the order
// (permissions first, every environment; Development-only bootstrap/demo seeders behind a hard runtime
// guard, not just DI registration). Runs before the app starts serving requests.
await app.Services.SeedDatabaseAsync(app.Environment);

// Must run before anything that reads Request.Scheme/Connection.RemoteIpAddress (HTTPS redirection,
// the refresh-token cookie's Secure flag, IP-partitioned rate limiting, request logging) — see
// AddForwardedHeadersForTrustedProxy's doc comment for the trusted-proxy configuration model.
app.UseForwardedHeaders();

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
app.MapAuthEndpoints();
app.MapPlatformAuthEndpoints();
app.MapPlatformOrganizationEndpoints();
app.MapTenantEndpoints();
app.MapRoleEndpoints();
app.MapUserEndpoints();
app.MapPropertyEndpoints();
app.MapFlatOwnershipEndpoints();
app.MapResidentEndpoints();
app.MapExpenseTypeEndpoints();
app.MapExpenseEndpoints();
app.MapMeEndpoints();
app.MapAttachmentEndpoints();
app.MapGuestEndpoints();
app.MapVehicleEndpoints();
app.MapAccessSessionEndpoints();
app.MapDomesticWorkerEndpoints();
app.MapServiceProviderEndpoints();
app.MapStaffAttendanceEndpoints();
app.MapSebaVisitorEndpoints();
app.MapParcelEndpoints();
app.MapSecurityReportEndpoints();
app.MapResidentAccountEndpoints();
app.MapPaymentEndpoints();
app.MapServiceChargeRuleEndpoints();
app.MapInvoiceEndpoints();
app.MapFineEndpoints();
app.MapFinancialReportEndpoints();
app.MapFinanceEndpoints();
app.MapMeterEndpoints();
app.MapRatePlanEndpoints();
app.MapReadingEndpoints();
app.MapUtilityReportEndpoints();
app.MapFacilityEndpoints();
app.MapFacilityBookingEndpoints();
app.MapPoolEndpoints();
app.MapFacilityReportEndpoints();
app.MapGeneratorEndpoints();
app.MapGeneratorSessionEndpoints();
app.MapGeneratorFuelReceiptEndpoints();
app.MapGeneratorMaintenanceEndpoints();
app.MapGeneratorReportEndpoints();
app.MapGasCylinderSupplierEndpoints();
app.MapCylinderPurchaseEndpoints();
app.MapCylinderStockEndpoints();
app.MapGasCylinderReportEndpoints();
app.MapOccupancyRegistrationEndpoints();

// Scalar UI + the raw OpenAPI JSON document expose the full internal API surface (every endpoint,
// DTO shape, route pattern) — reconnaissance value for an attacker even though it doesn't bypass auth
// on the endpoints themselves. Development-only; this does not affect the underlying OpenAPI
// generation the frontend's codegen:api script depends on, which reads the file mycondo-web checks in
// (openapi/mycondo-api.json), regenerated locally against a Development-mode instance.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("MyCondo API")
            .WithTheme(ScalarTheme.BluePlanet);
    });

    app.MapGet("/", () => Results.Redirect("/scalar"));
}

await app.RunAsync();

public partial class Program { }
