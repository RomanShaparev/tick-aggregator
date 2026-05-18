using System.ComponentModel.DataAnnotations;

namespace TickAggregator.Infrastructure.Configuration;

public sealed class DeduplicationOptions
{
    public const string Section = "Deduplication";

    [Range(1, double.MaxValue)]
    public double TtlSeconds { get; init; }
}
