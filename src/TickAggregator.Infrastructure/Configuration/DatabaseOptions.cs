namespace TickAggregator.Infrastructure.Configuration;

public sealed class DatabaseOptions
{
    public const string Section = "Database";
    public string ConnectionString { get; set; } = default!;
}
