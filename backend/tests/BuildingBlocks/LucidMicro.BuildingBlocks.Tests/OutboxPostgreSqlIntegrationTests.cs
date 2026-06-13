using LucidMicro.BuildingBlocks.Outbox.Abstractions.Models;
using LucidMicro.BuildingBlocks.Outbox.EFCore.Entities;
using LucidMicro.BuildingBlocks.Outbox.EFCore.ModelBuilding;
using LucidMicro.BuildingBlocks.Outbox.EFCore.Options;
using LucidMicro.BuildingBlocks.Outbox.EFCore.Stores;
using LucidMicro.Tests.Shared.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace LucidMicro.BuildingBlocks.Tests;

public sealed class OutboxPostgreSqlIntegrationTests
{
    private const string ConnectionStringEnvironmentVariable = "LUCID_TEST_POSTGRESQL_CONNECTION_STRING";
    private const string SchemaName = "lucid_outbox_tests";

    [PostgreSqlFact]
    public async Task ClaimPendingAsync_UsesPostgreSqlClaimSql()
    {
        var now = DateTimeOffset.Parse("2026-05-26T00:00:00+00:00");
        var connectionString = GetTestConnectionString();
        await using var database = await PostgreSqlOutboxDatabase.CreateAsync(
            connectionString,
            SchemaName);
        await using var seedContext = database.CreateContext();
        var seedStore = CreateStore(seedContext, now);
        var first = CreateMessage("first", DateTimeOffset.Parse("2026-05-26T00:00:00+00:00"));
        var second = CreateMessage("second", DateTimeOffset.Parse("2026-05-26T00:01:00+00:00"));

        await seedStore.AddAsync(first);
        await seedStore.AddAsync(second);
        await seedStore.SaveChangesAsync();

        await using var firstContext = database.CreateContext();
        await using var secondContext = database.CreateContext();
        var firstStore = CreateStore(firstContext, now);
        var secondStore = CreateStore(secondContext, now);

        var results = await Task.WhenAll(
            firstStore.ClaimPendingAsync(maxCount: 1),
            secondStore.ClaimPendingAsync(maxCount: 1));

        var claimedIds = results
            .Select(messages => Assert.Single(messages).Id)
            .ToArray();

        Assert.Contains(first.Id, claimedIds);
        Assert.Contains(second.Id, claimedIds);
        Assert.Equal(2, claimedIds.Distinct().Count());
    }

    private static EfCoreOutboxMessageStore<TestOutboxDbContext> CreateStore(
        TestOutboxDbContext dbContext,
        DateTimeOffset now)
    {
        return new EfCoreOutboxMessageStore<TestOutboxDbContext>(
            dbContext,
            new TestTimeProvider(now),
            new EfCoreOutboxOptions());
    }

    private static OutboxMessage CreateMessage(
        string name,
        DateTimeOffset createdAt)
    {
        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = $"test.{name}.v1",
            OccurredAt = DateTimeOffset.Parse("2026-05-26T00:00:00+00:00"),
            Payload = $$"""{"name":"{{name}}"}""",
            CreatedAt = createdAt
        };
    }

    private static string GetTestConnectionString()
    {
        return Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable)
            ?? throw new InvalidOperationException(
                $"{ConnectionStringEnvironmentVariable} is required for PostgreSQL integration tests.");
    }

    private sealed class PostgreSqlFactAttribute : FactAttribute
    {
        public PostgreSqlFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(
                    Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable)))
            {
                Skip = $"{ConnectionStringEnvironmentVariable} is not set.";
            }
        }
    }

    private sealed class PostgreSqlOutboxDatabase : IAsyncDisposable
    {
        private readonly string _baseConnectionString;
        private readonly string _schemaName;
        private readonly string _scopedConnectionString;

        private PostgreSqlOutboxDatabase(
            string baseConnectionString,
            string schemaName,
            string scopedConnectionString)
        {
            _baseConnectionString = baseConnectionString;
            _schemaName = schemaName;
            _scopedConnectionString = scopedConnectionString;
        }

        public static async Task<PostgreSqlOutboxDatabase> CreateAsync(
            string connectionString,
            string schemaName)
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            await using (var command = connection.CreateCommand())
            {
                command.CommandText = $"""
                    drop schema if exists {QuoteIdentifier(schemaName)} cascade;
                    create schema {QuoteIdentifier(schemaName)};
                    """;
                await command.ExecuteNonQueryAsync();
            }

            var connectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString)
            {
                SearchPath = schemaName
            };
            var database = new PostgreSqlOutboxDatabase(
                connectionString,
                schemaName,
                connectionStringBuilder.ConnectionString);

            await using var context = database.CreateContext();
            await context.GetService<IRelationalDatabaseCreator>().CreateTablesAsync();

            return database;
        }

        public TestOutboxDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<TestOutboxDbContext>()
                .UseNpgsql(_scopedConnectionString)
                .Options;

            return new TestOutboxDbContext(options);
        }

        public async ValueTask DisposeAsync()
        {
            await using var connection = new NpgsqlConnection(_baseConnectionString);
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = $"drop schema if exists {QuoteIdentifier(_schemaName)} cascade;";
            await command.ExecuteNonQueryAsync();
        }

        private static string QuoteIdentifier(string value)
        {
            return "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
        }
    }

    private sealed class TestOutboxDbContext : DbContext
    {
        public TestOutboxDbContext(DbContextOptions<TestOutboxDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ConfigureOutbox();
        }
    }
}
