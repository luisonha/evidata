using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Evidata.Modules.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sessions",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RolesSnapshot = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    PermissionsVersion = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastAccessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sessions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_sessions_expires_at",
                schema: "identity",
                table: "sessions",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "idx_sessions_id_unique",
                schema: "identity",
                table: "sessions",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_sessions_revoked_at",
                schema: "identity",
                table: "sessions",
                column: "RevokedAt");

            migrationBuilder.CreateIndex(
                name: "idx_sessions_tenant_user",
                schema: "identity",
                table: "sessions",
                columns: new[] { "TenantId", "UserId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sessions",
                schema: "identity");
        }
    }
}
