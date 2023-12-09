using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArbZaqqweeBot.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedDB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BaseUrl",
                table: "Exchangers");

            migrationBuilder.DropColumn(
                name: "TickersEndpoint",
                table: "Exchangers");

            migrationBuilder.AlterColumn<decimal>(
                name: "Volume",
                table: "Tickers",
                type: "numeric",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShortName",
                table: "Networks",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShortName",
                table: "Networks");

            migrationBuilder.AlterColumn<decimal>(
                name: "Volume",
                table: "Tickers",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<string>(
                name: "BaseUrl",
                table: "Exchangers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TickersEndpoint",
                table: "Exchangers",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
