using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Evidata.Modules.TenantManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialTenantManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Settings_TimeZone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Settings_Locale = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Settings_MaxUsers = table.Column<int>(type: "integer", nullable: false),
                    Settings_MfaRequired = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenants", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tenants_Slug",
                table: "tenants",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tenants");
        }
    }
}
