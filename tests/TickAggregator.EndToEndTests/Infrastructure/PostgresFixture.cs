using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using TickAggregator.Domain.Entities;
using TickAggregator.Infrastructure.Database;

namespace TickAggregator.EndToEndTests.Infrastructure;

public sealed class PostgresFixture
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task StartAsync()
    {
        await _container.StartAsync();
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public async Task<int> CountTicksAsync()
    {
        await using var context = CreateContext();
        return await context.Ticks.CountAsync();
    }

    public async Task<IReadOnlyList<Tick>> GetTicksAsync()
    {
        await using var context = CreateContext();
        return await context.Ticks
            .AsNoTracking()
            .OrderBy(t => t.Exchange)
            .ThenBy(t => t.TradeId)
            .ToListAsync();
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
