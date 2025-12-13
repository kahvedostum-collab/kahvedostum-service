using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KahveDostum_Service.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixReceiptCafeNullableAndDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Receipts_Cafes_CafeId",
                table: "Receipts");

            migrationBuilder.DropColumn(
                name: "Date",
                table: "Receipts");

            migrationBuilder.DropColumn(
                name: "Time",
                table: "Receipts");

            migrationBuilder.AlterColumn<string>(
                name: "Total",
                table: "Receipts",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "Receipts",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            // 🔥 KRİTİK: CafeId NULLABLE
            migrationBuilder.AlterColumn<int>(
                name: "CafeId",
                table: "Receipts",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<DateTime>(
                name: "ReceiptDate",
                table: "Receipts",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            migrationBuilder.AddForeignKey(
                name: "FK_Receipts_Cafes_CafeId",
                table: "Receipts",
                column: "CafeId",
                principalTable: "Cafes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Receipts_Cafes_CafeId",
                table: "Receipts");

            migrationBuilder.DropColumn(
                name: "ReceiptDate",
                table: "Receipts");

            migrationBuilder.AlterColumn<string>(
                name: "Total",
                table: "Receipts",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "Receipts",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Date",
                table: "Receipts",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Time",
                table: "Receipts",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            // 🔥 ÖNCE CafeId NOT NULL
            migrationBuilder.AlterColumn<int>(
                name: "CafeId",
                table: "Receipts",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            // 🔥 SONRA FK
            migrationBuilder.AddForeignKey(
                name: "FK_Receipts_Cafes_CafeId",
                table: "Receipts",
                column: "CafeId",
                principalTable: "Cafes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

    }
}
