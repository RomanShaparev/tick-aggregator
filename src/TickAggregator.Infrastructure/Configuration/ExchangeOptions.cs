using System.ComponentModel.DataAnnotations;

namespace TickAggregator.Infrastructure.Configuration;

public sealed class DataSourceConfig
{
    public const string Section = "DataSources";
    
    [Required]
    public required string Name { get; init; }

    [Required]
    public required string Url { get; init; }

    [Range(0, int.MaxValue)]
    public int ReconnectDelayMs { get; init; } = 5000;

    [Range(0, int.MaxValue)]
    public int MaxReconnectDelayMs { get; init; } = 60000;

    [Range(1, int.MaxValue)]
    public int MessageBufferSize { get; init; } = 10000;
}
