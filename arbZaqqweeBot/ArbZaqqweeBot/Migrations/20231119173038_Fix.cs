﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArbZaqqweeBot.Migrations
{
    /// <inheritdoc />
    public partial class Fix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "isValid",
                table: "Pairs",
                newName: "IsValid");

            migrationBuilder.RenameColumn(
                name: "isSend",
                table: "Pairs",
                newName: "IsSend");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsValid",
                table: "Pairs",
                newName: "isValid");

            migrationBuilder.RenameColumn(
                name: "IsSend",
                table: "Pairs",
                newName: "isSend");
        }
    }
}
