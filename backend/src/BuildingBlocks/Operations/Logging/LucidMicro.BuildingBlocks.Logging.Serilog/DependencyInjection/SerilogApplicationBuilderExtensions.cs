using System.Diagnostics;
using LucidMicro.BuildingBlocks.Logging.SerilogIntegration.Options;
using LucidMicro.BuildingBlocks.Logging.SerilogIntegration.RequestLogging;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Sinks.Grafana.Loki;

namespace LucidMicro.BuildingBlocks.Logging.SerilogIntegration.DependencyInjection;

public static class SerilogApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddLucidSerilog(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.AddLucidSerilog(
            builder.Configuration.GetRequiredSection(LucidSerilogOptions.ConfigurationSectionName));
    }

    public static WebApplicationBuilder AddLucidSerilog(
        this WebApplicationBuilder builder,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);

        return builder.AddLucidSerilog(options => configuration.Bind(options));
    }

    public static WebApplicationBuilder AddLucidSerilog(
        this WebApplicationBuilder builder,
        Action<LucidSerilogOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configureOptions);

        var options = new LucidSerilogOptions();
        configureOptions(options);
        options.Validate();

        builder.Host.UseSerilog((_, _, loggerConfiguration) =>
        {
            loggerConfiguration
                .ReadFrom.Configuration(builder.Configuration)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithProperty("ApplicationName", options.ApplicationName)
                .Enrich.WithProperty("EnvironmentName", builder.Environment.EnvironmentName)
                .Enrich.WithSpan();

            loggerConfiguration.WriteTo.Console(
                outputTemplate: options.OutputTemplate);

            if (options.File.Enabled)
            {
                loggerConfiguration.WriteTo.File(
                    path: options.File.Path,
                    rollingInterval: Enum.Parse<RollingInterval>(options.File.RollingInterval, ignoreCase: true),
                    retainedFileCountLimit: options.File.RetainedFileCountLimit,
                    outputTemplate: options.OutputTemplate);
            }

            if (options.Loki.Enabled)
            {
                loggerConfiguration.WriteTo.GrafanaLoki(
                    options.Loki.Uri,
                    labels: CreateLokiLabels(options, builder.Environment.EnvironmentName));
            }
        });

        return builder;
    }

    public static WebApplication UseLucidSerilogRequestLogging(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var options = new LucidSerilogOptions();
        app.Configuration.GetRequiredSection(LucidSerilogOptions.ConfigurationSectionName).Bind(options);
        options.Validate();

        app.UseSerilogRequestLogging(requestLoggingOptions =>
        {
            requestLoggingOptions.MessageTemplate = options.RequestLogging.MessageTemplate;
            requestLoggingOptions.GetLevel = LucidSerilogRequestLogging.GetLevel;
            requestLoggingOptions.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("TraceId", Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier);
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
            };
        });

        return app;
    }

    private static IReadOnlyCollection<LokiLabel> CreateLokiLabels(
        LucidSerilogOptions options,
        string environmentName)
    {
        var labels = new Dictionary<string, string>(options.Loki.Labels, StringComparer.Ordinal)
        {
            ["application"] = options.ApplicationName,
            ["environment"] = environmentName
        };

        return labels
            .Select(static label => new LokiLabel
            {
                Key = label.Key,
                Value = label.Value
            })
            .ToArray();
    }
}
