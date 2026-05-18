using Microsoft.Extensions.Logging;
using TickAggregator.Domain.Entities;
using TickAggregator.Domain.Enums;

namespace TickAggregator.Infrastructure.Parsers;

public sealed class ExchangeParserService
{
    private readonly IReadOnlyDictionary<Exchange, IExchangeParser> _parsers;
    private readonly ILogger<ExchangeParserService> _logger;

    public ExchangeParserService(IEnumerable<IExchangeParser> parsers, ILogger<ExchangeParserService> logger)
    {
        _parsers = parsers.ToDictionary(p => p.Exchange);
        _logger = logger;
    }

    public IReadOnlyList<Tick> Parse(Exchange exchange, string payload)
    {
        if (!_parsers.TryGetValue(exchange, out var parser))
        {
            _logger.LogWarning("No parser for exchange {Exchange}", exchange);
            return [];
        }

        try
        {
            return parser.Parse(payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse message from {Exchange}", exchange);
            return [];
        }
    }
}
