using System.Text;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TickAggregator.Domain.Entities;
using TickAggregator.Domain.Interfaces;

namespace TickAggregator.Infrastructure.Database;

public sealed class TickRepository : ITickRepository
{
    private readonly AppDbContext _context;

    public TickRepository(AppDbContext context)
        => _context = context;

    public async Task InsertBatchAsync(IReadOnlyList<Tick> ticks, CancellationToken ct)
    {
        if (ticks.Count == 0) return;

        const int chunkSize = 500;
        foreach (var chunk in ticks.Chunk(chunkSize))
            await InsertChunkAsync(chunk, ct);
    }

    private async Task InsertChunkAsync(Tick[] chunk, CancellationToken ct)
    {
        var sb = new StringBuilder(
            "INSERT INTO ticks (exchange, trade_id, ticker, price, volume, timestamp) VALUES ");
        var parameters = new List<NpgsqlParameter>(chunk.Length * 6);

        for (int i = 0; i < chunk.Length; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append($"(@e{i}, @t{i}, @k{i}, @p{i}, @v{i}, @ts{i})");

            var tick = chunk[i];
            parameters.Add(new NpgsqlParameter($"@e{i}", tick.Exchange.ToString()));
            parameters.Add(new NpgsqlParameter($"@t{i}", tick.TradeId));
            parameters.Add(new NpgsqlParameter($"@k{i}", tick.Ticker));
            parameters.Add(new NpgsqlParameter($"@p{i}", tick.Price));
            parameters.Add(new NpgsqlParameter($"@v{i}", tick.Volume));
            parameters.Add(new NpgsqlParameter($"@ts{i}", tick.Timestamp));
        }

        sb.Append(" ON CONFLICT DO NOTHING");
        await _context.Database.ExecuteSqlRawAsync(sb.ToString(), parameters.Cast<object>(), ct);
    }
}
