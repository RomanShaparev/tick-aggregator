using Dapper;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;
using TickAggregator.Infrastructure.Database;
using Xunit;

namespace TickAggregator.IntegrationTests.Infrastructure;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("tickaggregator_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        var options = new DbContextOptionsBuilder<TickDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        await using var context = new TickDbContext(options);
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    public async Task<int> CountTicksAsync()
    {
        await using var conn = new NpgsqlConnection(ConnectionString);
        return await conn.QuerySingleAsync<int>("SELECT COUNT(*) FROM ticks");
    }

    public async Task ClearTicksAsync()
    {
        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.ExecuteAsync("TRUNCATE TABLE ticks");
    }
}
