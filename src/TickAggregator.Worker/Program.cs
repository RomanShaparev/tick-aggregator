using Microsoft.EntityFrameworkCore;
using TickAggregator.Infrastructure;
using TickAggregator.Infrastructure.Configuration;
using TickAggregator.Infrastructure.Database;
using TickAggregator.Worker.Extensions;
using TickAggregator.Worker.Workers;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) =>
    {
        services.AddValidatedOptions<RabbitMqOptions>(RabbitMqOptions.Section);
        services.AddValidatedOptions<DatabaseOptions>(DatabaseOptions.Section);
        services.AddValidatedOptions<DeduplicationOptions>(DeduplicationOptions.Section);

        services.AddInfrastructure();

        var dataSources = ctx.Configuration.GetValidatedOptions<List<DataSourceConfig>>(DataSourceConfig.Section);
        if (dataSources == null || dataSources.Count == 0)
            throw new Exception($"{DataSourceConfig.Section} is empty.");
        
        services.AddDataSources(dataSources);

        services.AddHostedService<ExchangeCollectorWorker>();
        services.AddHostedService<StatisticsWorker>();
    })
    .Build();

await using (var scope = host.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

await host.RunAsync();