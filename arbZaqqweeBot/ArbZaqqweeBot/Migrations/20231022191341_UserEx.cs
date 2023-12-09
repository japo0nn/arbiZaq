using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArbZaqqweeBot.Migrations
{
    /// <inheritdoc />
    public partial class UserEx : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ToRun",
                table: "UserExchangers",
                newName: "IsEnabled");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsEnabled",
                table: "UserExchangers",
                newName: "ToRun");
        }
    }
}
