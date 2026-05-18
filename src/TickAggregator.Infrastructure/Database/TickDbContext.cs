using Microsoft.EntityFrameworkCore;
using TickAggregator.Domain.Entities;

namespace TickAggregator.Infrastructure.Database;

public sealed class TickDbContext : DbContext
{
    public DbSet<Tick> Ticks => Set<Tick>();

    public TickDbContext(DbContextOptions<TickDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(TickDbContext).Assembly);
}
