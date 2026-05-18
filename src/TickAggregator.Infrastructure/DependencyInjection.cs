using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TickAggregator.Application.Interfaces;
using TickAggregator.Application.Metrics;
using TickAggregator.Application.Services;
using TickAggregator.Domain.Enums;
using TickAggregator.Domain.Interfaces;
using TickAggregator.Infrastructure.Caching;
using TickAggregator.Infrastructure.Configuration;
using TickAggregator.Infrastructure.Database;
using TickAggregator.Infrastructure.DataSources;
using TickAggregator.Infrastructure.DataSources.Binance;
using TickAggregator.Infrastructure.DataSources.Bybit;
using TickAggregator.Infrastructure.DataSources.Kraken;
using TickAggregator.Infrastructure.Deduplication;
using TickAggregator.Infrastructure.Messaging;
using TickAggregator.Infrastructure.WebSocket;

namespace TickAggregator.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddDbContext<AppDbContext>((sp, options) =>
            options.UseNpgsql(
                sp.GetRequiredService<IOptions<DatabaseOptions>>().Value.ConnectionString,
                o => o.EnableRetryOnFailure()));

        services.AddScoped<ITickRepository, TickRepository>();
        services.AddMemoryCache();
        services.AddSingleton<ICache, InMemoryCache>();
        services.AddSingleton<IDeduplicationService, DeduplicationService>();
        services.AddSingleton<TickMetrics>();

        services.AddScoped<TickProcessingService>();

        services.AddSingleton<ITickProducer, TickProducer>();
        services.AddSingleton<IDlqProducer, DlqProducer>();
        services.AddSingleton<TickCollectorService>();

        services.AddMassTransitServices();

        return services;
    }

    private static IServiceCollection AddMassTransitServices(this IServiceCollection services)
    {
        services.AddMassTransit(x =>
        {
            x.AddConsumer<TickBatchConsumer>();

            x.UsingRabbitMq((busCtx, cfg) =>
            {
                var mq = busCtx.GetRequiredService<IOptions<RabbitMqOptions>>().Value;

                cfg.Host(mq.Host, mq.Port, mq.VirtualHost, h =>
                {
                    h.Username(mq.Username);
                    h.Password(mq.Password);
                });

                cfg.ReceiveEndpoint(mq.QueueName, e =>
                {
                    e.Durable = true;
                    e.AutoDelete = false;
                    e.PrefetchCount = mq.PrefetchCount;
                    e.ConfigureConsumer<TickBatchConsumer>(busCtx, c =>
                        c.Options<BatchOptions>(o => o
                            .SetMessageLimit(mq.BatchMessageLimit)
                            .SetTimeLimit(TimeSpan.FromMilliseconds(mq.BatchTimeLimitMs))
                            .SetConcurrencyLimit(1)));
                });
            });
        });

        return services;
    }

    public static IServiceCollection AddDataSources(this IServiceCollection services,
        IEnumerable<DataSourceConfig> configs)
    {
        foreach (var cfg in configs)
        {
            var exchange = Enum.Parse<Exchange>(cfg.Name);
            var uri = new Uri(cfg.Url);

            if (uri.Scheme is not ("ws" or "wss"))
                throw new NotSupportedException($"Protocol '{uri.Scheme}' is not supported for exchange {exchange}.");

            var initialDelay = TimeSpan.FromMilliseconds(cfg.ReconnectDelayMs);
            var maxDelay = TimeSpan.FromMilliseconds(cfg.MaxReconnectDelayMs);
            var messageBufferSize = cfg.MessageBufferSize;

            IExchangeDataSource Factory(IServiceProvider sp)
            {
                var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
                var dlq = sp.GetRequiredService<IDlqProducer>();
                var wsClient = new ExchangeWebSocketClient(uri, initialDelay, maxDelay, messageBufferSize,
                    loggerFactory.CreateLogger($"{nameof(ExchangeWebSocketClient)}.{exchange}"));
                var logger = loggerFactory.CreateLogger($"{nameof(ExchangeWebSocketDataSource<object>)}.{exchange}");
                return exchange switch
                {
                    Exchange.Binance => new ExchangeWebSocketDataSource<BinanceTick>(
                        wsClient, new BinanceTickParser(), new BinanceTickMapper(), dlq, exchange, logger),
                    Exchange.Bybit => new ExchangeWebSocketDataSource<BybitTick>(
                        wsClient, new BybitTickParser(), new BybitTickMapper(), dlq, exchange, logger),
                    Exchange.Kraken => new ExchangeWebSocketDataSource<KrakenTick>(
                        wsClient, new KrakenTickParser(), new KrakenTickMapper(), dlq, exchange, logger),
                    _ => throw new NotSupportedException($"Exchange {exchange} is not supported.")
                };
            }

            services.AddSingleton<IExchangeDataSource>(Factory);
        }

        return services;
    }
}