using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TickAggregator.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ticks",
                columns: table => new
                {
                    trade_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    exchange = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ticker = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    volume = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    timestamp = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    received_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ticks", x => new { x.exchange, x.trade_id });
                });

            migrationBuilder.CreateIndex(
                name: "IX_ticks_exchange_timestamp",
                table: "ticks",
                columns: new[] { "exchange", "timestamp" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ticks");
        }
    }
}
