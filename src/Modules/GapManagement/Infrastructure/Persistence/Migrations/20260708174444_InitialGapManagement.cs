using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Evidata.Modules.GapManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialGapManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "gap");

            migrationBuilder.CreateTable(
                name: "compliance_gaps",
                schema: "gap",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_module = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    source_entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    legal_obligation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: true),
                    due_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    risk_acceptance_justification = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<Guid>(type: "uuid", nullable: true),
                    last_modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    closed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    closed_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_compliance_gaps", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_compliance_gaps_owner",
                schema: "gap",
                table: "compliance_gaps",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "ix_compliance_gaps_source",
                schema: "gap",
                table: "compliance_gaps",
                columns: new[] { "tenant_id", "source_module", "source_entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_compliance_gaps_tenant_severity",
                schema: "gap",
                table: "compliance_gaps",
                columns: new[] { "tenant_id", "severity" });

            migrationBuilder.CreateIndex(
                name: "ix_compliance_gaps_tenant_status",
                schema: "gap",
                table: "compliance_gaps",
                columns: new[] { "tenant_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "compliance_gaps",
                schema: "gap");
        }
    }
}
