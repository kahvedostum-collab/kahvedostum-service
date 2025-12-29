using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KahveDostum_Service.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVeryfiAccountsAndFixReceiptHashIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VeryfiAccounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BaseUrl = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ClientId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Username = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ApiKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UsedCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    UsageLimit = table.Column<int>(type: "int", nullable: false, defaultValue: 100),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VeryfiAccounts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VeryfiAccounts_IsActive_UsedCount",
                table: "VeryfiAccounts",
                columns: new[] { "IsActive", "UsedCount" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VeryfiAccounts");
        }
    }
}
