using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TickAggregator.Domain.Entities;
using TickAggregator.Domain.Enums;
using TickAggregator.Infrastructure.Database;
using TickAggregator.IntegrationTests.Infrastructure;
using Xunit;

namespace TickAggregator.IntegrationTests;

public sealed class DatabaseTests : IAsyncLifetime
{
    private readonly PostgresFixture _fixture = new();

    public Task InitializeAsync() => _fixture.InitializeAsync();
    public Task DisposeAsync() => _fixture.DisposeAsync();

    private TickRepository CreateRepository()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;
        return new TickRepository(new AppDbContext(options));
    }

    [Fact]
    public async Task InsertBatch_SingleTick_Persisted()
    {
        await _fixture.ClearTicksAsync();
        var repo = CreateRepository();
        var tick = new Tick
        {
            TradeId = "t1",
            Exchange = Exchange.Binance,
            Ticker = "BTCUSDT",
            Price = 50000m,
            Volume = 0.001m,
            Timestamp = DateTimeOffset.UtcNow,
        };

        await repo.InsertBatchAsync([tick], CancellationToken.None);

        var count = await _fixture.CountTicksAsync();
        count.Should().Be(1);
    }

    [Fact]
    public async Task InsertBatch_100Ticks_AllPersisted()
    {
        await _fixture.ClearTicksAsync();
        var repo = CreateRepository();
        var ticks = Enumerable.Range(1, 100).Select(i => new Tick
        {
            TradeId = $"batch-{i}",
            Exchange = Exchange.Binance,
            Ticker = "BTCUSDT",
            Price = 50000m + i,
            Volume = 0.001m,
            Timestamp = DateTimeOffset.UtcNow,
        }).ToList();

        await repo.InsertBatchAsync(ticks, CancellationToken.None);

        var count = await _fixture.CountTicksAsync();
        count.Should().Be(100);
    }

    [Fact]
    public async Task InsertBatch_EmptyList_DoesNothing()
    {
        await _fixture.ClearTicksAsync();
        var repo = CreateRepository();

        await repo.InsertBatchAsync([], CancellationToken.None);

        var count = await _fixture.CountTicksAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task InsertBatch_DuplicatePrimaryKey_IgnoresConflict()
    {
        await _fixture.ClearTicksAsync();
        var repo = CreateRepository();
        var tick = new Tick
        {
            TradeId = "dup-001",
            Exchange = Exchange.Binance,
            Ticker = "BTCUSDT",
            Price = 50000m,
            Volume = 0.001m,
            Timestamp = DateTimeOffset.UtcNow,
        };

        await repo.InsertBatchAsync([tick], CancellationToken.None);
        var act = async () => await repo.InsertBatchAsync([tick], CancellationToken.None);

        await act.Should().NotThrowAsync();
        var count = await _fixture.CountTicksAsync();
        count.Should().Be(1);
    }

    [Fact]
    public async Task InsertBatch_SameTradeIdDifferentExchanges_BothPersisted()
    {
        await _fixture.ClearTicksAsync();
        var repo = CreateRepository();
        var ticks = new[]
        {
            new Tick { TradeId = "shared-id", Exchange = Exchange.Binance, Ticker = "BTCUSDT", Price = 50000m, Volume = 0.001m, Timestamp = DateTimeOffset.UtcNow },
            new Tick { TradeId = "shared-id", Exchange = Exchange.Kraken, Ticker = "BTCUSD",  Price = 50001m, Volume = 0.002m, Timestamp = DateTimeOffset.UtcNow },
        };

        await repo.InsertBatchAsync(ticks, CancellationToken.None);

        var count = await _fixture.CountTicksAsync();
        count.Should().Be(2);
    }

    [Fact]
    public async Task InsertBatch_MultipleExchanges_AllPersisted()
    {
        await _fixture.ClearTicksAsync();
        var repo = CreateRepository();
        var ticks = new[]
        {
            new Tick { TradeId = "x-1", Exchange = Exchange.Binance, Ticker = "BTCUSDT", Price = 50000m, Volume = 0.1m, Timestamp = DateTimeOffset.UtcNow },
            new Tick { TradeId = "x-2", Exchange = Exchange.Kraken,  Ticker = "BTCUSD",  Price = 50001m, Volume = 0.2m, Timestamp = DateTimeOffset.UtcNow },
            new Tick { TradeId = "x-3", Exchange = Exchange.Bybit,   Ticker = "BTCUSDT", Price = 49999m, Volume = 0.3m, Timestamp = DateTimeOffset.UtcNow },
        };

        await repo.InsertBatchAsync(ticks, CancellationToken.None);

        var count = await _fixture.CountTicksAsync();
        count.Should().Be(3);
    }
}
