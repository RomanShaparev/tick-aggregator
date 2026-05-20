using TickAggregator.Infrastructure;
using TickAggregator.Infrastructure.Configuration;
using TickAggregator.Worker.Extensions;
using TickAggregator.Worker.Workers;

namespace TickAggregator.Worker;

public static class HostBuilderFactory
{
    public static IHostBuilder Create(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((ctx, services) =>
            {
                services.AddValidatedOptions<RabbitMqOptions>(RabbitMqOptions.Section);
                services.AddValidatedOptions<DatabaseOptions>(DatabaseOptions.Section);
                services.AddValidatedOptions<DeduplicationOptions>(DeduplicationOptions.Section);

                services.AddInfrastructure();

                var dataSources = ctx.Configuration.GetValidatedOptions<List<DataSourceConfig>>(DataSourceConfig.Section) ?? [];
                services.AddDataSources(dataSources);

                services.AddHostedService<ExchangeCollectorWorker>();
                services.AddHostedService<StatisticsWorker>();
            });
}
