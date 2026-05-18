using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TickAggregator.Application.Metrics;
using TickAggregator.Application.Services;
using TickAggregator.Domain.Enums;
using TickAggregator.Domain.Interfaces;
using TickAggregator.Infrastructure.Configuration;
using TickAggregator.Infrastructure.DataSources;
using TickAggregator.Infrastructure.Database;
using TickAggregator.Infrastructure.Deduplication;
using TickAggregator.Infrastructure.Messaging;
using TickAggregator.Infrastructure.Parsers;
using TickAggregator.Infrastructure.Parsers.Binance;
using TickAggregator.Infrastructure.Parsers.Bybit;
using TickAggregator.Infrastructure.Parsers.Kraken;

namespace TickAggregator.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddDbContext<TickDbContext>((sp, options) =>
            options.UseNpgsql(sp.GetRequiredService<IOptions<DatabaseOptions>>().Value.ConnectionString));

        services.AddScoped<ITickRepository, TickRepository>();
        services.AddSingleton<IDeduplicationService, InMemoryDeduplicationService>();
        services.AddSingleton<TickMetrics>();

        services.AddSingleton<IExchangeParser, BinanceParser>();
        services.AddSingleton<IExchangeParser, KrakenParser>();
        services.AddSingleton<IExchangeParser, BybitParser>();
        services.AddSingleton<ExchangeParserService>();

        services.AddScoped<TickProcessingService>();

        services.AddSingleton<IMessageProducer, MassTransitMessageProducer>();

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

            switch (uri.Scheme)
            {
                case "ws" or "wss":
                    services.AddSingleton<IExchangeDataSource>(sp => new WebSocketExchangeDataSource(
                        exchange, uri, cfg.ReconnectDelayMs, cfg.MaxReconnectDelayMs,
                        sp.GetRequiredService<ILoggerFactory>()));
                    break;
                default:
                    throw new NotSupportedException(
                        $"Protocol '{uri.Scheme}' is not supported for exchange {exchange}.");
            }
        }

        return services;
    }
}