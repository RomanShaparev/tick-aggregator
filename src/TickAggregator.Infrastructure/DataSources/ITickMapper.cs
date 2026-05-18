using TickAggregator.Domain.Entities;

namespace TickAggregator.Infrastructure.DataSources;

public interface ITickMapper<T>
{
    Tick Map(T exchangeTick);
}
