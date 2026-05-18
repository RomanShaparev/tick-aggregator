namespace TickAggregator.Infrastructure.DataSources;

public interface IParser<T>
{
    IEnumerable<T> Parse(string payload);
}
