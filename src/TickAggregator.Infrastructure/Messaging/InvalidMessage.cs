using TickAggregator.Domain.Enums;

namespace TickAggregator.Infrastructure.Messaging;

public sealed record InvalidMessage(
    Exchange Exchange,
    string RawPayload
);
