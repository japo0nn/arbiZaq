using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArbZaqqweeBot.Migrations
{
    /// <inheritdoc />
    public partial class BidAskPrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Price",
                table: "Tickers",
                newName: "SellPrice");

            migrationBuilder.AddColumn<decimal>(
                name: "BuyPrice",
                table: "Tickers",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BuyPrice",
                table: "Tickers");

            migrationBuilder.RenameColumn(
                name: "SellPrice",
                table: "Tickers",
                newName: "Price");
        }
    }
}
