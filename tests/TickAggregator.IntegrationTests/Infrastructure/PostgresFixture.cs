using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using TickAggregator.Infrastructure.Database;
using Xunit;

namespace TickAggregator.IntegrationTests.Infrastructure;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    public async Task<int> CountTicksAsync()
    {
        await using var context = CreateContext();
        return await context.Ticks.CountAsync();
    }

    public async Task ClearTicksAsync()
    {
        await using var context = CreateContext();
        await context.Ticks.ExecuteDeleteAsync();
    }

    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new AppDbContext(options);
    }
}