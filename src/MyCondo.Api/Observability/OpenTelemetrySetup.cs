using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace MyCondo.Api.Observability;

public static class OpenTelemetrySetup
{
    public static IServiceCollection AddMyCondoOpenTelemetry(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        const string ServiceName = "mycondo-api";
        string? otlpEndpoint = configuration["MYCONDO_OTLP_ENDPOINT"]; // e.g. http://otel-collector:4317

        services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(serviceName: ServiceName))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation(opt => opt.RecordException = true)
                    .AddHttpClientInstrumentation();
                // EF Core OTel instrumentation is prerelease as of 2026-05; add when stable.

                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    tracing.AddOtlpExporter(opt => opt.Endpoint = new Uri(otlpEndpoint));
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();
                // OpenTelemetry.Instrumentation.Runtime can be added later to capture GC/threadpool/JIT counters.

                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    metrics.AddOtlpExporter(opt => opt.Endpoint = new Uri(otlpEndpoint));
                }
            });

        return services;
    }
}
