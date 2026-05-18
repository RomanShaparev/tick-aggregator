using TickAggregator.Domain.Entities;
using TickAggregator.Domain.Enums;

namespace TickAggregator.Infrastructure.Parsers;

public interface IExchangeParser
{
    Exchange Exchange { get; }
    IReadOnlyList<Tick> Parse(string rawMessage);
}
