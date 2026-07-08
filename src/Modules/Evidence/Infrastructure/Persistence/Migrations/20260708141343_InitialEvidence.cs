using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Evidata.Modules.Evidence.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialEvidence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "evidence");

            migrationBuilder.CreateTable(
                name: "evidences",
                schema: "evidence",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    sensitivity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    blob_path = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    content_type = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    size_bytes = table.Column<long>(type: "bigint", nullable: true),
                    supersedes_evidence_id = table.Column<Guid>(type: "uuid", nullable: true),
                    tags = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deletion_reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evidences", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_evidences_sensitivity",
                schema: "evidence",
                table: "evidences",
                column: "sensitivity");

            migrationBuilder.CreateIndex(
                name: "ix_evidences_supersedes",
                schema: "evidence",
                table: "evidences",
                column: "supersedes_evidence_id");

            migrationBuilder.CreateIndex(
                name: "ix_evidences_tenant_id",
                schema: "evidence",
                table: "evidences",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_evidences_tenant_status",
                schema: "evidence",
                table: "evidences",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_evidences_tenant_type",
                schema: "evidence",
                table: "evidences",
                columns: new[] { "tenant_id", "type" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "evidences",
                schema: "evidence");
        }
    }
}
