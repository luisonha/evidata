using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Evidata.Modules.Evidence.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEvidencePackJob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "evidence_pack_jobs",
                schema: "evidence",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_by = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    result_blob_path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    error_message = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    evidence_ids = table.Column<List<Guid>>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evidence_pack_jobs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_evidence_pack_jobs_expires",
                schema: "evidence",
                table: "evidence_pack_jobs",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_evidence_pack_jobs_tenant",
                schema: "evidence",
                table: "evidence_pack_jobs",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_evidence_pack_jobs_tenant_status",
                schema: "evidence",
                table: "evidence_pack_jobs",
                columns: new[] { "tenant_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "evidence_pack_jobs",
                schema: "evidence");
        }
    }
}
