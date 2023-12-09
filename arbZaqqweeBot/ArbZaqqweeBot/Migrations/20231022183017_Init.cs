using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArbZaqqweeBot.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Exchangers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    BaseUrl = table.Column<string>(type: "text", nullable: false),
                    TickersEndpoint = table.Column<string>(type: "text", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exchangers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Networks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExchangerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Coin = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Fee = table.Column<decimal>(type: "numeric", nullable: true),
                    DepositEnable = table.Column<bool>(type: "boolean", nullable: false),
                    WithdrawEnable = table.Column<bool>(type: "boolean", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Networks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Networks_Exchangers_ExchangerId",
                        column: x => x.ExchangerId,
                        principalTable: "Exchangers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Tickers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExchangerId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Symbol = table.Column<string>(type: "text", nullable: false),
                    BuyPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    SellPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tickers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tickers_Exchangers_ExchangerId",
                        column: x => x.ExchangerId,
                        principalTable: "Exchangers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Pairs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BuyTickerId = table.Column<Guid>(type: "uuid", nullable: false),
                    SellTickerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Spread = table.Column<decimal>(type: "numeric", nullable: false),
                    isSend = table.Column<bool>(type: "boolean", nullable: false),
                    isValid = table.Column<bool>(type: "boolean", nullable: false),
                    DateCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pairs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pairs_Tickers_BuyTickerId",
                        column: x => x.BuyTickerId,
                        principalTable: "Tickers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Pairs_Tickers_SellTickerId",
                        column: x => x.SellTickerId,
                        principalTable: "Tickers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TickerNets",
                columns: table => new
                {
                    TickerId = table.Column<Guid>(type: "uuid", nullable: false),
                    NetworkId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TickerNets", x => new { x.TickerId, x.NetworkId });
                    table.ForeignKey(
                        name: "FK_TickerNets_Networks_NetworkId",
                        column: x => x.NetworkId,
                        principalTable: "Networks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TickerNets_Tickers_TickerId",
                        column: x => x.TickerId,
                        principalTable: "Tickers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Networks_ExchangerId",
                table: "Networks",
                column: "ExchangerId");

            migrationBuilder.CreateIndex(
                name: "IX_Pairs_BuyTickerId",
                table: "Pairs",
                column: "BuyTickerId");

            migrationBuilder.CreateIndex(
                name: "IX_Pairs_SellTickerId",
                table: "Pairs",
                column: "SellTickerId");

            migrationBuilder.CreateIndex(
                name: "IX_TickerNets_NetworkId",
                table: "TickerNets",
                column: "NetworkId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickers_ExchangerId",
                table: "Tickers",
                column: "ExchangerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Pairs");

            migrationBuilder.DropTable(
                name: "TickerNets");

            migrationBuilder.DropTable(
                name: "Networks");

            migrationBuilder.DropTable(
                name: "Tickers");

            migrationBuilder.DropTable(
                name: "Exchangers");
        }
    }
}
