using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TickAggregator.Infrastructure.Database;

public sealed class TickDbContextFactory : IDesignTimeDbContextFactory<TickDbContext>
{
    public TickDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<TickDbContext>()
            .UseNpgsql(Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                       ?? "Host=localhost;Database=tickaggregator;Username=postgres;Password=postgres")
            .Options;

        return new TickDbContext(options);
    }
}
