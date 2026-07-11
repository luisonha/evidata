using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Evidata.Modules.Evidence.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEvidenceRequirementAndValidation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "evidence_requirements",
                schema: "evidence",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    processing_activity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    review_domain = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    is_blocking = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evidence_requirements", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "evidence_validations",
                schema: "evidence",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    evidence_requirement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    evidence_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    validation_comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    validated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    validated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evidence_validations", x => x.id);
                    table.ForeignKey(
                        name: "FK_evidence_validations_evidence_requirements_evidence_require~",
                        column: x => x.evidence_requirement_id,
                        principalSchema: "evidence",
                        principalTable: "evidence_requirements",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_evidence_validations_evidences_evidence_id",
                        column: x => x.evidence_id,
                        principalSchema: "evidence",
                        principalTable: "evidences",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_evidence_requirements_review_domain",
                schema: "evidence",
                table: "evidence_requirements",
                column: "review_domain");

            migrationBuilder.CreateIndex(
                name: "ix_evidence_requirements_tenant_activity",
                schema: "evidence",
                table: "evidence_requirements",
                columns: new[] { "tenant_id", "processing_activity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_evidence_requirements_tenant_id",
                schema: "evidence",
                table: "evidence_requirements",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_evidence_validations_evidence_id",
                schema: "evidence",
                table: "evidence_validations",
                column: "evidence_id");

            migrationBuilder.CreateIndex(
                name: "ix_evidence_validations_requirement_status",
                schema: "evidence",
                table: "evidence_validations",
                columns: new[] { "evidence_requirement_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_evidence_validations_status",
                schema: "evidence",
                table: "evidence_validations",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_evidence_validations_tenant_id",
                schema: "evidence",
                table: "evidence_validations",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_evidence_validations_tenant_requirement",
                schema: "evidence",
                table: "evidence_validations",
                columns: new[] { "tenant_id", "evidence_requirement_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "evidence_validations",
                schema: "evidence");

            migrationBuilder.DropTable(
                name: "evidence_requirements",
                schema: "evidence");
        }
    }
}
