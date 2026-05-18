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
using TickAggregator.Infrastructure.Configuration;
using TickAggregator.Infrastructure.Database;
using TickAggregator.Infrastructure.DataSources.Binance;
using TickAggregator.Infrastructure.DataSources.Bybit;
using TickAggregator.Infrastructure.DataSources.Kraken;
using TickAggregator.Infrastructure.Caching;
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

            IExchangeDataSource Factory(IServiceProvider sp)
            {
                var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
                return exchange switch
                {
                    Exchange.Binance => new BinanceWebSocketDataSource(
                        new ExchangeWebSocketClient(uri, initialDelay, maxDelay,
                            loggerFactory.CreateLogger($"{nameof(ExchangeWebSocketClient)}.Binance")),
                        new BinanceParser(),
                        new BinanceMapper(),
                        loggerFactory.CreateLogger<BinanceWebSocketDataSource>()),
                    Exchange.Bybit => new BybitWebSocketDataSource(
                        new ExchangeWebSocketClient(uri, initialDelay, maxDelay,
                            loggerFactory.CreateLogger($"{nameof(ExchangeWebSocketClient)}.Bybit")),
                        new BybitParser(),
                        new BybitMapper(),
                        loggerFactory.CreateLogger<BybitWebSocketDataSource>()),
                    Exchange.Kraken => new KrakenWebSocketDataSource(
                        new ExchangeWebSocketClient(uri, initialDelay, maxDelay,
                            loggerFactory.CreateLogger($"{nameof(ExchangeWebSocketClient)}.Kraken")),
                        new KrakenParser(),
                        new KrakenMapper(),
                        loggerFactory.CreateLogger<KrakenWebSocketDataSource>()),
                    _ => throw new NotSupportedException($"Exchange {exchange} is not supported.")
                };
            }

            services.AddSingleton<IExchangeDataSource>(Factory);
        }

        return services;
    }
}
