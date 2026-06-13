using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LucidMicro.Tests.Shared.Persistence;

public sealed class SqliteDbContextScope<TDbContext> : IAsyncDisposable
    where TDbContext : DbContext
{
    private readonly SqliteConnection _connection;

    private SqliteDbContextScope(SqliteConnection connection, TDbContext context)
    {
        _connection = connection;
        Context = context;
    }

    public TDbContext Context { get; }

    public static async Task<SqliteDbContextScope<TDbContext>> CreateAsync(
        Func<SqliteConnection, TDbContext> contextFactory)
    {
        ArgumentNullException.ThrowIfNull(contextFactory);

        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        try
        {
            var context = contextFactory(connection);
            await context.Database.EnsureCreatedAsync();

            return new SqliteDbContextScope<TDbContext>(connection, context);
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
