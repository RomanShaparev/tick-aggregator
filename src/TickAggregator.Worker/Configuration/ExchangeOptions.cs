namespace TickAggregator.Worker.Configuration;

public sealed class ExchangeOptions
{
    public const string Section = "Exchanges";
    public List<ExchangeConfig> Items { get; set; } = [];
}

public sealed class ExchangeConfig
{
    public string Name { get; set; } = default!;
    public string WebSocketUrl { get; set; } = default!;
    public int ReconnectDelayMs { get; set; } = 5000;
    public int MaxReconnectDelayMs { get; set; } = 60000;
}
