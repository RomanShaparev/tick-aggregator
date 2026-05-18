using System.Text.Json;

namespace TickAggregator.Infrastructure.DataSources.Kraken;

public sealed class KrakenParser : IParser<KrakenTick>
{
    public IEnumerable<KrakenTick> Parse(string payload)
    {
        var message = JsonSerializer.Deserialize<KrakenMessage>(payload)!;
        return message.Data is { Length: > 0 } ? message.Data : [];
    }
}
