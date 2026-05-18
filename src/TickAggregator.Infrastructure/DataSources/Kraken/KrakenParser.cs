using System.Text.Json;

namespace TickAggregator.Infrastructure.DataSources.Kraken;

public sealed class KrakenParser : IParser<KrakenTick>
{
    public bool TryParse(string payload, out IEnumerable<KrakenTick> items)
    {
        try
        {
            var message = JsonSerializer.Deserialize<KrakenMessage>(payload)!;
            items = message.Data is { Length: > 0 } ? message.Data : [];
            return true;
        }
        catch
        {
            items = [];
            return false;
        }
    }
}
