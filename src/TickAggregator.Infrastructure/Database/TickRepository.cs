using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
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

        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await _context.BulkInsertOrUpdateAsync(
                ticks.ToList(),
                new BulkConfig { ConflictOption = ConflictOption.Ignore },
                cancellationToken: ct);
        });
    }
}