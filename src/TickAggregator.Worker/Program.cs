using MassTransit;
using Microsoft.EntityFrameworkCore;
using TickAggregator.Infrastructure.Configuration;
using TickAggregator.Infrastructure.Database;
using TickAggregator.Infrastructure.Extensions;
using TickAggregator.Infrastructure.Messaging;
using TickAggregator.Worker.Workers;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) =>
    {
        services.AddOptions<RabbitMqOptions>()
            .Bind(ctx.Configuration.GetSection(RabbitMqOptions.Section))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<DatabaseOptions>()
            .Bind(ctx.Configuration.GetSection(DatabaseOptions.Section))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddInfrastructure();

        var mq = ctx.Configuration.GetSection(RabbitMqOptions.Section).Get<RabbitMqOptions>()
                 ?? throw new InvalidOperationException($"Configuration section '{RabbitMqOptions.Section}' is missing.");
        var dataSources = ctx.Configuration.GetSection("DataSources").Get<List<DataSourceConfig>>() ?? [];

        services.AddDataSources(dataSources);

        services.AddMassTransit(x =>
        {
            x.AddConsumer<RawTickBatchConsumer>(c =>
                c.Options<BatchOptions>(o => o
                    .SetMessageLimit(mq.BatchMessageLimit)
                    .SetTimeLimit(TimeSpan.FromMilliseconds(mq.BatchTimeLimitMs))
                    .SetConcurrencyLimit(1)));

            x.UsingRabbitMq((busCtx, cfg) =>
            {
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
                    e.ConfigureConsumer<RawTickBatchConsumer>(busCtx);
                });
            });
        });

        services.AddHostedService<ExchangeCollectorWorker>();
        services.AddHostedService<StatisticsWorker>();
    })
    .Build();

await using (var scope = host.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TickDbContext>();
    await db.Database.MigrateAsync();
}

await host.RunAsync();
