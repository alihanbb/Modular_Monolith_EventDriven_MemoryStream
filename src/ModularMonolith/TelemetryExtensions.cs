using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using ModularMonolith.Shared.Infrastructure.Telemetry;

namespace ModularMonolith;

public static class TelemetryExtensions
{
    public static IServiceCollection AddTelemetry(this IServiceCollection services, IConfiguration configuration)
    {
        var jaegerEndpoint = configuration["Jaeger:Endpoint"] ?? "http://localhost:4317";

        services.AddOpenTelemetry()
            .ConfigureResource(r => r
                .AddService(
                    serviceName: Tracing.ServiceName,
                    serviceVersion: Tracing.Version))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation(opts =>
                {
                    opts.RecordException = true;
                })
                .AddHttpClientInstrumentation()
                .AddSource(Tracing.ActivitySource.Name)
                .AddOtlpExporter(opts =>
                {
                    opts.Endpoint = new Uri(jaegerEndpoint);
                }));

        return services;
    }
}
