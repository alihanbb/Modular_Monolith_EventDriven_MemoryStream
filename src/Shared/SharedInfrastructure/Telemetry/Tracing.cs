using System.Diagnostics;

namespace ModularMonolith.Shared.Infrastructure.Telemetry;

public static class Tracing
{
    public const string ServiceName = "ModularMonolith";
    public const string Version = "1.0.0";

    public static readonly ActivitySource ActivitySource = new(ServiceName, Version);
}
