using System.Text.Json;

namespace TickAggregator.Infrastructure.DataSources.Bybit;

public sealed class BybitParser : IParser<BybitTick>
{
    public bool TryParse(string payload, out IEnumerable<BybitTick> items)
    {
        try
        {
            var message = JsonSerializer.Deserialize<BybitMessage>(payload)!;
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
