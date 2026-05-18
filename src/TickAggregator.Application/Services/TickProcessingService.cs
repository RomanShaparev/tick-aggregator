using Microsoft.Extensions.Logging;
using TickAggregator.Application.Metrics;
using TickAggregator.Domain.Entities;
using TickAggregator.Domain.Interfaces;

namespace TickAggregator.Application.Services;

public sealed class TickProcessingService
{
    private readonly IDeduplicationService _deduplication;
    private readonly ITickRepository _repository;
    private readonly TickMetrics _metrics;
    private readonly ILogger<TickProcessingService> _logger;

    public TickProcessingService(
        IDeduplicationService deduplication,
        ITickRepository repository,
        TickMetrics metrics,
        ILogger<TickProcessingService> logger)
    {
        _deduplication = deduplication;
        _repository = repository;
        _metrics = metrics;
        _logger = logger;
    }

    public async Task ProcessBatchAsync(IReadOnlyList<Tick> ticks, CancellationToken ct)
    {
        var uniqueTicks = new List<Tick>(ticks.Count);
        foreach (var tick in ticks)
        {
            if (!_deduplication.IsDuplicate(tick.Exchange, tick.TradeId))
                uniqueTicks.Add(tick);
        }

        if (uniqueTicks.Count == 0)
            return;

        await _repository.InsertBatchAsync(uniqueTicks, ct);

        foreach (var tick in uniqueTicks)
            _metrics.TicksSaved.Add(1, new KeyValuePair<string, object?>("exchange", tick.Exchange.ToString()));

        _logger.LogDebug("Saved {Saved} ticks ({Deduped} deduped)", uniqueTicks.Count, ticks.Count - uniqueTicks.Count);
    }
}