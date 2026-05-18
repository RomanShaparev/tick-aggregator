using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TickAggregator.Domain.Entities;

namespace TickAggregator.Infrastructure.Database;

public sealed class TickEntityConfiguration : IEntityTypeConfiguration<Tick>
{
    public void Configure(EntityTypeBuilder<Tick> builder)
    {
        builder.ToTable("ticks");

        builder.HasKey(t => new { t.Exchange, t.TradeId });

        builder.Property(t => t.Exchange)
            .HasColumnName("exchange")
            .HasMaxLength(50)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(t => t.TradeId)
            .HasColumnName("trade_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(t => t.Ticker)
            .HasColumnName("ticker")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.Price)
            .HasColumnName("price")
            .HasColumnType("numeric(18,8)")
            .IsRequired();

        builder.Property(t => t.Volume)
            .HasColumnName("volume")
            .HasColumnType("numeric(18,8)")
            .IsRequired();

        builder.Property(t => t.Timestamp)
            .HasColumnName("timestamp")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(t => t.ReceivedAt)
            .HasColumnName("received_at")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.HasIndex(t => new { t.Exchange, t.Timestamp })
            .IsDescending(false, true);
    }
}
