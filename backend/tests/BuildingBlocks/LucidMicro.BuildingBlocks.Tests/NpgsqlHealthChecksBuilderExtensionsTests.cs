using LucidMicro.BuildingBlocks.HealthChecks.AspNetCore;
using LucidMicro.BuildingBlocks.HealthChecks.Npgsql;
using LucidMicro.BuildingBlocks.HealthChecks.Npgsql.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace LucidMicro.BuildingBlocks.Tests;

public sealed class NpgsqlHealthChecksBuilderExtensionsTests
{
    [Fact]
    public void AddLucidNpgsqlDbContextCheck_RegistersReadyDatabaseCheck()
    {
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(options => options.UseSqlite("Data Source=:memory:"));

        services.AddHealthChecks()
            .AddLucidNpgsqlDbContextCheck<TestDbContext>();

        using var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value;
        var registration = Assert.Single(options.Registrations);

        Assert.Equal(LucidHealthCheckTags.PostgreSql, registration.Name);
        Assert.Equal(typeof(NpgsqlDbContextHealthCheck<TestDbContext>), registration.Factory(serviceProvider).GetType());
        Assert.Contains(LucidHealthCheckTags.Ready, registration.Tags);
        Assert.Contains(LucidHealthCheckTags.Database, registration.Tags);
        Assert.Contains(LucidHealthCheckTags.PostgreSql, registration.Tags);
    }

    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options)
            : base(options)
        {
        }
    }
}
