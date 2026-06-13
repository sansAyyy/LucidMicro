using LucidMicro.BuildingBlocks.Observability.OpenTelemetry.DependencyInjection;
using LucidMicro.BuildingBlocks.Observability.OpenTelemetry.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace LucidMicro.BuildingBlocks.Tests;

public sealed class LucidOpenTelemetryOptionsTests
{
    [Fact]
    public void AddLucidOpenTelemetry_RegistersOptions()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationManager
        {
            ["Lucid:Observability:OpenTelemetry:ServiceName"] = "LucidMicro.Tests",
            ["Lucid:Observability:OpenTelemetry:ServiceVersion"] = "1.0.0",
            ["Lucid:Observability:OpenTelemetry:ServiceInstanceId"] = "test-instance",
            ["Lucid:Observability:OpenTelemetry:Metrics:Enabled"] = "false"
        };

        services.AddLucidOpenTelemetry(
            configuration.GetRequiredSection(LucidOpenTelemetryOptions.ConfigurationSectionName));

        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<LucidOpenTelemetryOptions>>().Value;

        Assert.Equal("LucidMicro.Tests", options.ServiceName);
        Assert.Equal("test-instance", options.ServiceInstanceId);
    }

    [Fact]
    public void AddLucidOpenTelemetry_RegistersOpenTelemetryHostedService()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration();

        services.AddLucidOpenTelemetry(
            configuration.GetRequiredSection(LucidOpenTelemetryOptions.ConfigurationSectionName));

        Assert.Contains(
            services,
            service => service.ServiceType == typeof(IHostedService)
                       && service.ImplementationType?.FullName?.Contains(
                           "OpenTelemetry",
                           StringComparison.Ordinal) == true);
    }

    [Fact]
    public void AddLucidOpenTelemetry_AcceptsConsoleAndOtlpExporters()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration();
        configuration["Lucid:Observability:OpenTelemetry:EnableConsoleExporter"] = "true";
        configuration["Lucid:Observability:OpenTelemetry:OtlpEndpoint"] = "http://localhost:4317";

        services.AddLucidOpenTelemetry(
            configuration.GetRequiredSection(LucidOpenTelemetryOptions.ConfigurationSectionName));

        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<LucidOpenTelemetryOptions>>().Value;

        Assert.True(options.EnableConsoleExporter);
        Assert.Equal("http://localhost:4317", options.OtlpEndpoint);
    }

    [Fact]
    public void AddLucidOpenTelemetry_AcceptsMetricsOptions()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration();
        configuration["Lucid:Observability:OpenTelemetry:Metrics:Enabled"] = "true";
        configuration["Lucid:Observability:OpenTelemetry:Metrics:EnableConsoleExporter"] = "true";

        services.AddLucidOpenTelemetry(
            configuration.GetRequiredSection(LucidOpenTelemetryOptions.ConfigurationSectionName));

        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<LucidOpenTelemetryOptions>>().Value;

        Assert.True(options.Metrics.Enabled);
        Assert.True(options.Metrics.EnableConsoleExporter);
    }

    [Fact]
    public void Metrics_AreEnabledByDefault()
    {
        var options = new LucidOpenTelemetryOptions
        {
            ServiceName = "LucidMicro.Tests"
        };

        Assert.True(options.Metrics.Enabled);
    }

    [Fact]
    public void AddLucidOpenTelemetry_Throws_WhenServiceNameIsMissing()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationManager
        {
            ["Lucid:Observability:OpenTelemetry:ServiceVersion"] = "1.0.0"
        };

        Assert.Throws<ArgumentException>(
            () => services.AddLucidOpenTelemetry(
                configuration.GetRequiredSection(LucidOpenTelemetryOptions.ConfigurationSectionName)));
    }

    [Fact]
    public void Validate_Throws_WhenServiceNameIsMissing()
    {
        var options = new LucidOpenTelemetryOptions();

        Assert.Throws<ArgumentException>(options.Validate);
    }

    [Fact]
    public void Validate_Throws_WhenOtlpEndpointIsInvalid()
    {
        var options = new LucidOpenTelemetryOptions
        {
            ServiceName = "LucidMicro.Tests",
            OtlpEndpoint = "localhost"
        };

        Assert.Throws<InvalidOperationException>(options.Validate);
    }

    private static ConfigurationManager CreateConfiguration()
    {
        return new ConfigurationManager
        {
            ["Lucid:Observability:OpenTelemetry:ServiceName"] = "LucidMicro.Tests",
            ["Lucid:Observability:OpenTelemetry:ServiceVersion"] = "1.0.0"
        };
    }
}
