using Microsoft.EntityFrameworkCore;
using TickAggregator.Infrastructure.Database;
using TickAggregator.Worker;

var host = HostBuilderFactory.Create(args).Build();

await using (var scope = host.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

await host.RunAsync();