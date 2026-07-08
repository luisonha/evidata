using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Evidata.Modules.Evidence.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEvidenceAccessLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "evidence_access_logs",
                schema: "evidence",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    evidence_id = table.Column<Guid>(type: "uuid", nullable: false),
                    accessed_by = table.Column<Guid>(type: "uuid", nullable: false),
                    accessed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    client_ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    sensitivity_at_access = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evidence_access_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_evidence_access_logs_evidences_evidence_id",
                        column: x => x.evidence_id,
                        principalSchema: "evidence",
                        principalTable: "evidences",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_evidence_access_logs_evidence_id",
                schema: "evidence",
                table: "evidence_access_logs",
                column: "evidence_id");

            migrationBuilder.CreateIndex(
                name: "ix_evidence_access_logs_tenant_date",
                schema: "evidence",
                table: "evidence_access_logs",
                columns: new[] { "tenant_id", "accessed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_evidence_access_logs_user",
                schema: "evidence",
                table: "evidence_access_logs",
                column: "accessed_by");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "evidence_access_logs",
                schema: "evidence");
        }
    }
}
