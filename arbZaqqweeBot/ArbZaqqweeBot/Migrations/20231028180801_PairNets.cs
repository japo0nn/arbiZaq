using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ArbZaqqweeBot.Migrations
{
    /// <inheritdoc />
    public partial class PairNets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BuyNetworkId",
                table: "Pairs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "DepositId",
                table: "Pairs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "SellNetworkId",
                table: "Pairs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "WithdrawId",
                table: "Pairs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Pairs_BuyNetworkId",
                table: "Pairs",
                column: "BuyNetworkId");

            migrationBuilder.CreateIndex(
                name: "IX_Pairs_DepositId",
                table: "Pairs",
                column: "DepositId");

            migrationBuilder.CreateIndex(
                name: "IX_Pairs_SellNetworkId",
                table: "Pairs",
                column: "SellNetworkId");

            migrationBuilder.CreateIndex(
                name: "IX_Pairs_WithdrawId",
                table: "Pairs",
                column: "WithdrawId");

            migrationBuilder.AddForeignKey(
                name: "FK_Pairs_Networks_BuyNetworkId",
                table: "Pairs",
                column: "BuyNetworkId",
                principalTable: "Networks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Pairs_Networks_DepositId",
                table: "Pairs",
                column: "DepositId",
                principalTable: "Networks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Pairs_Networks_SellNetworkId",
                table: "Pairs",
                column: "SellNetworkId",
                principalTable: "Networks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Pairs_Networks_WithdrawId",
                table: "Pairs",
                column: "WithdrawId",
                principalTable: "Networks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Pairs_Networks_BuyNetworkId",
                table: "Pairs");

            migrationBuilder.DropForeignKey(
                name: "FK_Pairs_Networks_DepositId",
                table: "Pairs");

            migrationBuilder.DropForeignKey(
                name: "FK_Pairs_Networks_SellNetworkId",
                table: "Pairs");

            migrationBuilder.DropForeignKey(
                name: "FK_Pairs_Networks_WithdrawId",
                table: "Pairs");

            migrationBuilder.DropIndex(
                name: "IX_Pairs_BuyNetworkId",
                table: "Pairs");

            migrationBuilder.DropIndex(
                name: "IX_Pairs_DepositId",
                table: "Pairs");

            migrationBuilder.DropIndex(
                name: "IX_Pairs_SellNetworkId",
                table: "Pairs");

            migrationBuilder.DropIndex(
                name: "IX_Pairs_WithdrawId",
                table: "Pairs");

            migrationBuilder.DropColumn(
                name: "BuyNetworkId",
                table: "Pairs");

            migrationBuilder.DropColumn(
                name: "DepositId",
                table: "Pairs");

            migrationBuilder.DropColumn(
                name: "SellNetworkId",
                table: "Pairs");

            migrationBuilder.DropColumn(
                name: "WithdrawId",
                table: "Pairs");
        }
    }
}
