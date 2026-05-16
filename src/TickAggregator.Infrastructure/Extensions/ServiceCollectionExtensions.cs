using Microsoft.Extensions.DependencyInjection;
using TickAggregator.Application.Interfaces;
using TickAggregator.Application.Services;
using TickAggregator.Domain.Interfaces;
using TickAggregator.Infrastructure.Database;
using TickAggregator.Infrastructure.Deduplication;
using TickAggregator.Infrastructure.Parsers;
using TickAggregator.Infrastructure.RabbitMq;

namespace TickAggregator.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<ITickRepository, TickRepository>();
        services.AddSingleton<IDeduplicationService, InMemoryDeduplicationService>();
        services.AddSingleton<ITickCounter, TickCounterService>();

        services.AddSingleton<IExchangeParser, BinanceParser>();
        services.AddSingleton<IExchangeParser, KrakenParser>();
        services.AddSingleton<IExchangeParser, BybitParser>();

        services.AddSingleton<TickProcessingService>();

        // IBus регистрируется через AddMassTransit в Program.cs
        services.AddSingleton<IMessageProducer, RabbitMqProducer>();

        return services;
    }
}
