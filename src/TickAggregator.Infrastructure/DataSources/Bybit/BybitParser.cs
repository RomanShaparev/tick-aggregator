using System.Text.Json;

namespace TickAggregator.Infrastructure.DataSources.Bybit;

public sealed class BybitParser : IParser<BybitTick>
{
    public IEnumerable<BybitTick> Parse(string payload)
    {
        var message = JsonSerializer.Deserialize<BybitMessage>(payload)!;
        return message.Data is { Length: > 0 } ? message.Data : [];
    }
}
