using TickAggregator.Domain.Entities;

namespace TickAggregator.Domain.Interfaces;

public interface IExchangeParser
{
    string ExchangeName { get; }
    IReadOnlyList<Tick> Parse(string rawMessage);
}
