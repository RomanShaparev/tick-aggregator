using System.ComponentModel.DataAnnotations;
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
            options.UseNpgsql(sp.GetRequiredService<IOptions<DatabaseOptions>>().Value.ConnectionString));

        services.AddScoped<ITickRepository, TickRepository>();
        services.AddMemoryCache();
        services.AddSingleton<ICache, InMemoryCache>();
        services.AddSingleton<IDeduplicationService, DeduplicationService>();
        services.AddSingleton<TickMetrics>();

        services.AddScoped<TickProcessingService>();

        services.AddSingleton<ITickProducer, TickProducer>();
        services.AddSingleton<IDlqProducer, DlqProducer>();
        services.AddSingleton<TickCollectorService>();

        return services;
    }

    public static IServiceCollection AddDataSources(this IServiceCollection services,
        IEnumerable<DataSourceConfig> configs)
    {
        foreach (var cfg in configs)
        {
            var results = new List<ValidationResult>();
            if (!Validator.TryValidateObject(cfg, new ValidationContext(cfg), results, validateAllProperties: true))
                throw new InvalidOperationException(
                    $"Invalid DataSource '{cfg.Name}': {string.Join("; ", results.Select(r => r.ErrorMessage))}");

            var exchange = Enum.Parse<Exchange>(cfg.Name);
            var uri = new Uri(cfg.Url);

            if (uri.Scheme is not ("ws" or "wss"))
                throw new NotSupportedException(
                    $"Protocol '{uri.Scheme}' is not supported for exchange {exchange}.");

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
                        wsClient, new BinanceParser(), new BinanceMapper(), dlq, exchange, logger),
                    Exchange.Bybit => new ExchangeWebSocketDataSource<BybitTick>(
                        wsClient, new BybitParser(), new BybitMapper(), dlq, exchange, logger),
                    Exchange.Kraken => new ExchangeWebSocketDataSource<KrakenTick>(
                        wsClient, new KrakenParser(), new KrakenMapper(), dlq, exchange, logger),
                    _ => throw new NotSupportedException($"Exchange {exchange} is not supported.")
                };
            }

            services.AddSingleton<IExchangeDataSource>(Factory);
        }

        return services;
    }
}
