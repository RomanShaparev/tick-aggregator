namespace TickAggregator.Infrastructure.DataSources;

public interface ITickParser<T>
{
    bool TryParse(string payload, out IEnumerable<T> items);
}
