using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArbZaqqweeBot.Migrations
{
    /// <inheritdoc />
    public partial class AddChainId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ChainId",
                table: "Networks",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChainId",
                table: "Networks");
        }
    }
}
