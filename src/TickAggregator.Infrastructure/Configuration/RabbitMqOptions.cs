namespace TickAggregator.Infrastructure.Configuration;

public sealed class RabbitMqOptions
{
    public const string Section = "RabbitMq";
    public string Host { get; set; } = "localhost";
    public ushort Port { get; set; } = 5672;
    public string VirtualHost { get; set; } = "/";
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string QueueName { get; set; } = "raw-ticks";
    public int BatchMessageLimit { get; set; } = 100;
    public int BatchTimeLimitMs { get; set; } = 500;
    public ushort PrefetchCount { get; set; } = 500;
}
