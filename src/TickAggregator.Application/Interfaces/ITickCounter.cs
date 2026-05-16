namespace TickAggregator.Application.Interfaces;

public interface ITickCounter
{
    void Increment(string exchange);
    long GetTotal();
    IReadOnlyDictionary<string, long> GetByExchange();
}
