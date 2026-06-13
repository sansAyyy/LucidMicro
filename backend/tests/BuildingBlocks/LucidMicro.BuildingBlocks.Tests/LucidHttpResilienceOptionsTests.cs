using LucidMicro.BuildingBlocks.Resilience.Http.DependencyInjection;
using LucidMicro.BuildingBlocks.Resilience.Http.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LucidMicro.BuildingBlocks.Tests;

public sealed class LucidHttpResilienceOptionsTests
{
    [Fact]
    public void AddLucidHttpResilience_RegistersOptions()
    {
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Lucid:Resilience:Http:TotalRequestTimeoutSeconds"] = "20",
            ["Lucid:Resilience:Http:AttemptTimeoutSeconds"] = "5",
            ["Lucid:Resilience:Http:Retry:MaxRetryAttempts"] = "2",
            ["Lucid:Resilience:Http:Retry:DelayMilliseconds"] = "100",
            ["Lucid:Resilience:Http:CircuitBreaker:FailureRatio"] = "0.25",
            ["Lucid:Resilience:Http:CircuitBreaker:MinimumThroughput"] = "10",
            ["Lucid:Resilience:Http:CircuitBreaker:SamplingDurationSeconds"] = "15",
            ["Lucid:Resilience:Http:CircuitBreaker:BreakDurationSeconds"] = "20"
        });
        var services = new ServiceCollection();

        services.AddLucidHttpResilience(
            configuration.GetRequiredSection(LucidHttpResilienceOptions.ConfigurationSectionName));

        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<LucidHttpResilienceOptions>>().Value;

        Assert.Equal(20, options.TotalRequestTimeoutSeconds);
        Assert.Equal(5, options.AttemptTimeoutSeconds);
        Assert.Equal(2, options.Retry.MaxRetryAttempts);
        Assert.Equal(100, options.Retry.DelayMilliseconds);
        Assert.Equal(0.25, options.CircuitBreaker.FailureRatio);
        Assert.Equal(10, options.CircuitBreaker.MinimumThroughput);
    }

    [Fact]
    public void AddLucidHttpResilience_Throws_WhenAttemptTimeoutExceedsTotalTimeout()
    {
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Lucid:Resilience:Http:TotalRequestTimeoutSeconds"] = "5",
            ["Lucid:Resilience:Http:AttemptTimeoutSeconds"] = "10"
        });
        var services = new ServiceCollection();

        Assert.Throws<InvalidOperationException>(() => services.AddLucidHttpResilience(
            configuration.GetRequiredSection(LucidHttpResilienceOptions.ConfigurationSectionName)));
    }

    [Fact]
    public void AddLucidStandardHttpResilienceHandler_RegistersNamedHttpClient()
    {
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Lucid:Resilience:Http:TotalRequestTimeoutSeconds"] = "20",
            ["Lucid:Resilience:Http:AttemptTimeoutSeconds"] = "5"
        });
        var services = new ServiceCollection();

        services
            .AddHttpClient("provider")
            .AddLucidStandardHttpResilienceHandler(
                configuration.GetRequiredSection(LucidHttpResilienceOptions.ConfigurationSectionName));

        using var serviceProvider = services.BuildServiceProvider();
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

        Assert.NotNull(httpClientFactory.CreateClient("provider"));
    }

    private static IConfigurationRoot CreateConfiguration(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
