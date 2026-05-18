using EFCore.BulkExtensions;
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

        await _context.BulkInsertAsync(ticks.ToList(),
            new BulkConfig { ConflictOption = ConflictOption.Ignore },
            cancellationToken: ct);
    }
}
