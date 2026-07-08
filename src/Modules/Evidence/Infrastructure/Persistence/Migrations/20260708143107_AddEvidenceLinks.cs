using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Evidata.Modules.Evidence.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEvidenceLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "evidence_links",
                schema: "evidence",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    evidence_id = table.Column<Guid>(type: "uuid", nullable: false),
                    linked_entity_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    linked_entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evidence_links", x => x.id);
                    table.ForeignKey(
                        name: "FK_evidence_links_evidences_evidence_id",
                        column: x => x.evidence_id,
                        principalSchema: "evidence",
                        principalTable: "evidences",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_evidence_links_dedup",
                schema: "evidence",
                table: "evidence_links",
                columns: new[] { "evidence_id", "linked_entity_type", "linked_entity_id", "deleted_at" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_evidence_links_tenant_evidence",
                schema: "evidence",
                table: "evidence_links",
                columns: new[] { "tenant_id", "evidence_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "evidence_links",
                schema: "evidence");
        }
    }
}
