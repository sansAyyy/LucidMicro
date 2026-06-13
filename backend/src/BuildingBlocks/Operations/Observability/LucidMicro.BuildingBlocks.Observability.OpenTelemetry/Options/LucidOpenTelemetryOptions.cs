namespace LucidMicro.BuildingBlocks.Observability.OpenTelemetry.Options;

public sealed class LucidOpenTelemetryOptions
{
    public const string ConfigurationSectionName = "Lucid:Observability:OpenTelemetry";

    public string ServiceName { get; set; } = string.Empty;

    public string ServiceVersion { get; set; } = "1.0.0";

    public string? ServiceInstanceId { get; set; }

    public string? OtlpEndpoint { get; set; }

    public bool EnableConsoleExporter { get; set; }

    public LucidOpenTelemetryMetricsOptions Metrics { get; set; } = new();

    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ServiceName);

        if (!string.IsNullOrWhiteSpace(OtlpEndpoint)
            && !Uri.TryCreate(OtlpEndpoint, UriKind.Absolute, out _))
        {
            throw new InvalidOperationException("OpenTelemetry OTLP endpoint must be an absolute URI.");
        }
    }
}

public sealed class LucidOpenTelemetryMetricsOptions
{
    public bool Enabled { get; set; } = true;

    public bool EnableConsoleExporter { get; set; }
}
