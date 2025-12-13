using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KahveDostum_Service.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReceiptHashAndFinalConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReceiptHash",
                table: "Receipts",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Receipts_ReceiptHash",
                table: "Receipts",
                column: "ReceiptHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Receipts_ReceiptHash",
                table: "Receipts");

            migrationBuilder.DropColumn(
                name: "ReceiptHash",
                table: "Receipts");
        }
    }
}
