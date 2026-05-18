using Microsoft.EntityFrameworkCore;
using TickAggregator.Domain.Entities;

namespace TickAggregator.Infrastructure.Database;

public sealed class AppDbContext : DbContext
{
    public DbSet<Tick> Ticks => Set<Tick>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}
