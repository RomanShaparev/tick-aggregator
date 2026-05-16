namespace TickAggregator.Domain.Interfaces;

public interface IDeduplicationService
{
    bool IsDuplicate(string exchange, string tradeId);
}
