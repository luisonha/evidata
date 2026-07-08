using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Evidata.Modules.Audit.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "audit");

            migrationBuilder.CreateTable(
                name: "audit_logs",
                schema: "audit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Action = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Resource = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Details = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Severity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_TenantId_OccurredAt",
                schema: "audit",
                table: "audit_logs",
                columns: new[] { "TenantId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_TenantId_Resource_ResourceId",
                schema: "audit",
                table: "audit_logs",
                columns: new[] { "TenantId", "Resource", "ResourceId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs",
                schema: "audit");
        }
    }
}
