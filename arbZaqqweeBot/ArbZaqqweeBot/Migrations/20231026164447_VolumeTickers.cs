using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArbZaqqweeBot.Migrations
{
    /// <inheritdoc />
    public partial class VolumeTickers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Volume",
                table: "Tickers",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Volume",
                table: "Tickers");
        }
    }
}
