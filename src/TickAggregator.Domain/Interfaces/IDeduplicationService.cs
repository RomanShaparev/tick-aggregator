using TickAggregator.Domain.Enums;

namespace TickAggregator.Domain.Interfaces;

public interface IDeduplicationService
{
    bool IsDuplicate(Exchange exchange, string tradeId);
}
