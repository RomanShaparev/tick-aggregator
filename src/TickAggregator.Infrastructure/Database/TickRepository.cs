using Dapper;
using Microsoft.Extensions.Options;
using Npgsql;
using TickAggregator.Domain.Entities;
using TickAggregator.Domain.Interfaces;
using TickAggregator.Infrastructure.Configuration;

namespace TickAggregator.Infrastructure.Database;

public sealed class TickRepository : ITickRepository
{
    private readonly string _connectionString;

    public TickRepository(IOptions<DatabaseOptions> options)
    {
        _connectionString = options.Value.ConnectionString;
    }

    public async Task InsertBatchAsync(IReadOnlyList<Tick> ticks, CancellationToken ct)
    {
        if (ticks.Count == 0) return;

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct);

        await using var writer = await connection.BeginBinaryImportAsync(
            "COPY ticks (trade_id, exchange, ticker, price, volume, timestamp, received_at) FROM STDIN (FORMAT BINARY)",
            ct);

        foreach (var tick in ticks)
        {
            await writer.StartRowAsync(ct);
            await writer.WriteAsync(tick.TradeId, NpgsqlTypes.NpgsqlDbType.Varchar, ct);
            await writer.WriteAsync(tick.Exchange, NpgsqlTypes.NpgsqlDbType.Varchar, ct);
            await writer.WriteAsync(tick.Ticker, NpgsqlTypes.NpgsqlDbType.Varchar, ct);
            await writer.WriteAsync(tick.Price, NpgsqlTypes.NpgsqlDbType.Numeric, ct);
            await writer.WriteAsync(tick.Volume, NpgsqlTypes.NpgsqlDbType.Numeric, ct);
            await writer.WriteAsync(tick.Timestamp, NpgsqlTypes.NpgsqlDbType.TimestampTz, ct);
            await writer.WriteAsync(tick.ReceivedAt, NpgsqlTypes.NpgsqlDbType.TimestampTz, ct);
        }

        await writer.CompleteAsync(ct);
    }

    public static async Task EnsureSchemaAsync(string connectionString, CancellationToken ct = default)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await connection.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS ticks (
                id          BIGSERIAL PRIMARY KEY,
                trade_id    VARCHAR(100) NOT NULL,
                exchange    VARCHAR(50)  NOT NULL,
                ticker      VARCHAR(20)  NOT NULL,
                price       NUMERIC(18,8) NOT NULL,
                volume      NUMERIC(18,8) NOT NULL,
                timestamp   TIMESTAMPTZ  NOT NULL,
                received_at TIMESTAMPTZ  NOT NULL,
                CONSTRAINT uq_ticks_exchange_trade UNIQUE (exchange, trade_id)
            );
            CREATE INDEX IF NOT EXISTS ix_ticks_exchange_timestamp ON ticks (exchange, timestamp DESC);
            """);
    }
}
