using System.Text.Json;
using TickAggregator.Domain.Entities;
using TickAggregator.Domain.Enums;
using TickAggregator.Infrastructure.Parsers;

namespace TickAggregator.Infrastructure.Parsers.Bybit;

public sealed class BybitParser : IExchangeParser
{
    public Exchange Exchange => Exchange.Bybit;

    public IReadOnlyList<Tick> Parse(string rawMessage)
    {
        var message = JsonSerializer.Deserialize<BybitMessage>(rawMessage)!;
        if (message.Data is not { Length: > 0 })
            return [];
        return message.Data.Select(t => t.ToTick(Exchange)).ToList();
    }
}
