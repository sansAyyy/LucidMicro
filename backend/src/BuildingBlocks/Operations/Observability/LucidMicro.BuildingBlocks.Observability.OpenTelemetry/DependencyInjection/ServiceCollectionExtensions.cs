using LucidMicro.BuildingBlocks.Observability.OpenTelemetry.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Resources;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace LucidMicro.BuildingBlocks.Observability.OpenTelemetry.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLucidOpenTelemetry(
        this IServiceCollection services,
        IConfigurationSection configurationSection)
    {
        ArgumentNullException.ThrowIfNull(configurationSection);

        return services.AddLucidOpenTelemetry(options => configurationSection.Bind(options));
    }

    public static IServiceCollection AddLucidOpenTelemetry(
        this IServiceCollection services,
        Action<LucidOpenTelemetryOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        var options = new LucidOpenTelemetryOptions();
        configureOptions(options);
        options.Validate();

        services
            .AddOptions<LucidOpenTelemetryOptions>()
            .Configure(configureOptions)
            .Validate(ValidateOptions, "Lucid OpenTelemetry options are invalid.")
            .ValidateOnStart();

        var openTelemetryBuilder = services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(
                options.ServiceName,
                serviceVersion: options.ServiceVersion,
                serviceInstanceId: GetServiceInstanceId(options)))
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource("LucidMicro.EventBus.RabbitMQ")
                    .AddAspNetCoreInstrumentation(instrumentationOptions =>
                    {
                        instrumentationOptions.Filter = ShouldTraceHttpRequest;
                        instrumentationOptions.RecordException = true;
                    })
                    .AddHttpClientInstrumentation();

                if (options.EnableConsoleExporter)
                {
                    tracing.AddConsoleExporter();
                }

                if (!string.IsNullOrWhiteSpace(options.OtlpEndpoint))
                {
                    tracing.AddOtlpExporter(exporterOptions =>
                    {
                        exporterOptions.Endpoint = new Uri(options.OtlpEndpoint);
                    });
                }
            });

        if (options.Metrics.Enabled)
        {
            openTelemetryBuilder.WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();

                if (options.Metrics.EnableConsoleExporter)
                {
                    metrics.AddConsoleExporter();
                }

                if (!string.IsNullOrWhiteSpace(options.OtlpEndpoint))
                {
                    metrics.AddOtlpExporter(exporterOptions =>
                    {
                        exporterOptions.Endpoint = new Uri(options.OtlpEndpoint);
                    });
                }
            });
        }

        return services;
    }

    private static bool ValidateOptions(LucidOpenTelemetryOptions options)
    {
        try
        {
            options.Validate();
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static string GetServiceInstanceId(LucidOpenTelemetryOptions options)
    {
        return string.IsNullOrWhiteSpace(options.ServiceInstanceId)
            ? Environment.MachineName
            : options.ServiceInstanceId;
    }

    private static bool ShouldTraceHttpRequest(HttpContext httpContext)
    {
        var path = httpContext.Request.Path;

        return !path.StartsWithSegments("/health")
               && !path.StartsWithSegments("/healthz")
               && !path.StartsWithSegments("/live")
               && !path.StartsWithSegments("/ready");
    }
}
