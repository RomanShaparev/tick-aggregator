namespace TickAggregator.Infrastructure.DataSources;

public interface IParser<T>
{
    bool TryParse(string payload, out IEnumerable<T> items);
}
