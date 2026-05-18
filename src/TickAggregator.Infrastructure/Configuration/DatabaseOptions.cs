using System.ComponentModel.DataAnnotations;

namespace TickAggregator.Infrastructure.Configuration;

public sealed class DatabaseOptions
{
    public const string Section = "Database";

    [Required]
    public required string ConnectionString { get; init; }
}
