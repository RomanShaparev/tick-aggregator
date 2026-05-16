using MassTransit;
using Microsoft.Extensions.Options;
using TickAggregator.Infrastructure.Configuration;
using TickAggregator.Infrastructure.Database;
using TickAggregator.Infrastructure.Extensions;
using TickAggregator.Infrastructure.RabbitMq;
using TickAggregator.Worker.Configuration;
using TickAggregator.Worker.Workers;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) =>
    {
        services.Configure<RabbitMqOptions>(ctx.Configuration.GetSection(RabbitMqOptions.Section));
        services.Configure<DatabaseOptions>(ctx.Configuration.GetSection(DatabaseOptions.Section));
        services.Configure<ExchangeOptions>(ctx.Configuration.GetSection(ExchangeOptions.Section));

        services.AddInfrastructure();

        var mq = ctx.Configuration.GetSection(RabbitMqOptions.Section).Get<RabbitMqOptions>()
                 ?? new RabbitMqOptions();

        // AddMassTransit регистрирует IBus (singleton) и запускает шину через IHostedService.
        // Шина стартует раньше ExchangeCollectorWorker (порядок регистрации).
        services.AddMassTransit(x =>
        {
            x.AddConsumer<RawTickBatchConsumer>(c =>
                c.Options<BatchOptions>(o => o
                    .SetMessageLimit(mq.BatchMessageLimit)
                    .SetTimeLimit(TimeSpan.FromMilliseconds(mq.BatchTimeLimitMs))
                    .SetConcurrencyLimit(1)));

            x.UsingRabbitMq((ctx, cfg) =>
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
                    e.ConfigureConsumer<RawTickBatchConsumer>(ctx);
                });
            });
        });

        services.AddHostedService<ExchangeCollectorWorker>();
        services.AddHostedService<StatisticsWorker>();
    })
    .Build();

var dbOptions = host.Services.GetRequiredService<IOptions<DatabaseOptions>>().Value;
await TickRepository.EnsureSchemaAsync(dbOptions.ConnectionString);

await host.RunAsync();
