using Dapper;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;
using TickAggregator.Infrastructure.Database;
using Xunit;

namespace TickAggregator.EndToEndTests.Infrastructure;

public sealed class PostgresFixture
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("tickaggregator_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task StartAsync()
    {
        await _container.StartAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        await using var context = new AppDbContext(options);
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public async Task<int> CountTicksAsync()
    {
        await using var conn = new NpgsqlConnection(ConnectionString);
        return await conn.QuerySingleAsync<int>("SELECT COUNT(*) FROM ticks");
    }
}
