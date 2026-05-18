using System.ComponentModel.DataAnnotations;

namespace TickAggregator.Infrastructure.Configuration;

public sealed class RabbitMqOptions
{
    public const string Section = "RabbitMq";

    [Required]
    public required string Host { get; init; }

    [Range(1, 65535)]
    public ushort Port { get; init; }

    [Required]
    public required string VirtualHost { get; init; }

    [Required]
    public required string Username { get; init; } 

    [Required]
    public required string Password { get; init; }

    [Required]
    public required string QueueName { get; init; } 

    [Range(1, int.MaxValue)]
    public int BatchMessageLimit { get; init; }

    [Range(1, int.MaxValue)]
    public int BatchTimeLimitMs { get; init; }

    [Range(1, 65535)]
    public ushort PrefetchCount { get; init; }
}
