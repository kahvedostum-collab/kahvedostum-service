using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KahveDostum_Service.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitCafeCompanySessionTokenFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ActivationTokenId",
                table: "UserSessions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "Cafes",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CafeActivationTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CafeId = table.Column<int>(type: "int", nullable: false),
                    ReceiptId = table.Column<int>(type: "int", nullable: true),
                    IssuedByUserId = table.Column<int>(type: "int", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CafeActivationTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CafeActivationTokens_Cafes_CafeId",
                        column: x => x.CafeId,
                        principalTable: "Cafes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CafeActivationTokens_Receipts_ReceiptId",
                        column: x => x.ReceiptId,
                        principalTable: "Receipts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CafeActivationTokens_Users_IssuedByUserId",
                        column: x => x.IssuedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_ActivationTokenId",
                table: "UserSessions",
                column: "ActivationTokenId");

            migrationBuilder.CreateIndex(
                name: "IX_Cafes_CompanyId",
                table: "Cafes",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_CafeActivationTokens_CafeId_IsUsed",
                table: "CafeActivationTokens",
                columns: new[] { "CafeId", "IsUsed" });

            migrationBuilder.CreateIndex(
                name: "IX_CafeActivationTokens_IssuedByUserId",
                table: "CafeActivationTokens",
                column: "IssuedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CafeActivationTokens_ReceiptId",
                table: "CafeActivationTokens",
                column: "ReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_CafeActivationTokens_Token",
                table: "CafeActivationTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Companies_Name",
                table: "Companies",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Cafes_Companies_CompanyId",
                table: "Cafes",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserSessions_CafeActivationTokens_ActivationTokenId",
                table: "UserSessions",
                column: "ActivationTokenId",
                principalTable: "CafeActivationTokens",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cafes_Companies_CompanyId",
                table: "Cafes");

            migrationBuilder.DropForeignKey(
                name: "FK_UserSessions_CafeActivationTokens_ActivationTokenId",
                table: "UserSessions");

            migrationBuilder.DropTable(
                name: "CafeActivationTokens");

            migrationBuilder.DropTable(
                name: "Companies");

            migrationBuilder.DropIndex(
                name: "IX_UserSessions_ActivationTokenId",
                table: "UserSessions");

            migrationBuilder.DropIndex(
                name: "IX_Cafes_CompanyId",
                table: "Cafes");

            migrationBuilder.DropColumn(
                name: "ActivationTokenId",
                table: "UserSessions");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Cafes");
        }
    }
}
