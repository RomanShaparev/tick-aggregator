using TickAggregator.Domain.Entities;

namespace TickAggregator.Infrastructure.DataSources;

public interface IMapper<T>
{
    Tick Map(T raw);
}
