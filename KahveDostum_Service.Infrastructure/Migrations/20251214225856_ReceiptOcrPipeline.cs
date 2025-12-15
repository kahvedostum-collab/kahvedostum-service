using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KahveDostum_Service.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReceiptOcrPipeline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Receipts_ReceiptHash",
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
                name: "ReceiptHash",
                table: "Receipts",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<DateTime>(
                name: "ReceiptDate",
                table: "Receipts",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<string>(
                name: "Bucket",
                table: "Receipts",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ClientLat",
                table: "Receipts",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ClientLng",
                table: "Receipts",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ObjectKey",
                table: "Receipts",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OcrJobId",
                table: "Receipts",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProcessedAt",
                table: "Receipts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectReason",
                table: "Receipts",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Receipts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "UploadedAt",
                table: "Receipts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ReceiptOcrResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReceiptId = table.Column<int>(type: "int", nullable: false),
                    JobId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RawText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Error = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceiptOcrResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReceiptOcrResults_Receipts_ReceiptId",
                        column: x => x.ReceiptId,
                        principalTable: "Receipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Receipts_ReceiptHash",
                table: "Receipts",
                column: "ReceiptHash",
                unique: true,
                filter: "[ReceiptHash] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Receipts_UserId_Status_CreatedAt",
                table: "Receipts",
                columns: new[] { "UserId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptOcrResults_CreatedAt",
                table: "ReceiptOcrResults",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ReceiptOcrResults_ReceiptId",
                table: "ReceiptOcrResults",
                column: "ReceiptId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReceiptOcrResults");

            migrationBuilder.DropIndex(
                name: "IX_Receipts_ReceiptHash",
                table: "Receipts");

            migrationBuilder.DropIndex(
                name: "IX_Receipts_UserId_Status_CreatedAt",
                table: "Receipts");

            migrationBuilder.DropColumn(
                name: "Bucket",
                table: "Receipts");

            migrationBuilder.DropColumn(
                name: "ClientLat",
                table: "Receipts");

            migrationBuilder.DropColumn(
                name: "ClientLng",
                table: "Receipts");

            migrationBuilder.DropColumn(
                name: "ObjectKey",
                table: "Receipts");

            migrationBuilder.DropColumn(
                name: "OcrJobId",
                table: "Receipts");

            migrationBuilder.DropColumn(
                name: "ProcessedAt",
                table: "Receipts");

            migrationBuilder.DropColumn(
                name: "RejectReason",
                table: "Receipts");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Receipts");

            migrationBuilder.DropColumn(
                name: "UploadedAt",
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
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ReceiptHash",
                table: "Receipts",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "ReceiptDate",
                table: "Receipts",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Receipts_ReceiptHash",
                table: "Receipts",
                column: "ReceiptHash",
                unique: true);
        }
    }
}
