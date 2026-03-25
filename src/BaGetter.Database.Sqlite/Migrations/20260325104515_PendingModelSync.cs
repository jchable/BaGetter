using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaGetter.Database.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class PendingModelSync : Migration
    {
        private const string DefaultTenantId = "default";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Create Tenants table first
            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Slug",
                table: "Tenants",
                column: "Slug",
                unique: true);

            // 2. Seed a default tenant so existing rows can reference it
            migrationBuilder.Sql($"""
                INSERT OR IGNORE INTO "Tenants" ("Id", "Name", "Slug", "CreatedAt")
                VALUES ('{DefaultTenantId}', 'Default', 'default', datetime('now'));
                """);

            // 3. Add TenantId to Packages with the default tenant as value
            migrationBuilder.DropIndex(
                name: "IX_Packages_Id_Version",
                table: "Packages");

            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "Packages",
                type: "TEXT",
                maxLength: 450,
                nullable: false,
                defaultValue: DefaultTenantId);

            // Backfill existing rows
            migrationBuilder.Sql($"""
                UPDATE "Packages" SET "TenantId" = '{DefaultTenantId}' WHERE "TenantId" = '' OR "TenantId" IS NULL;
                """);

            // 4. Add TenantId to AspNetUsers (nullable)
            migrationBuilder.AddColumn<string>(
                name: "TenantId",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: true);

            // Backfill existing users
            migrationBuilder.Sql($"""
                UPDATE "AspNetUsers" SET "TenantId" = '{DefaultTenantId}' WHERE "TenantId" IS NULL;
                """);

            // 5. Create indexes and foreign keys
            migrationBuilder.CreateIndex(
                name: "IX_Packages_TenantId_Id_Version",
                table: "Packages",
                columns: new[] { "TenantId", "Id", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_TenantId",
                table: "AspNetUsers",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Tenants_TenantId",
                table: "AspNetUsers",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Packages_Tenants_TenantId",
                table: "Packages",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Tenants_TenantId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_Packages_Tenants_TenantId",
                table: "Packages");

            migrationBuilder.DropTable(
                name: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_Packages_TenantId_Id_Version",
                table: "Packages");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_TenantId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Packages");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "AspNetUsers");

            migrationBuilder.CreateIndex(
                name: "IX_Packages_Id_Version",
                table: "Packages",
                columns: new[] { "Id", "Version" },
                unique: true);
        }
    }
}
