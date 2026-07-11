using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Evidata.Modules.GapManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FormalizeGapRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "gap_rule_id",
                schema: "gap",
                table: "compliance_gaps",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "gap_rules",
                schema: "gap",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rule_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    blocks_approval = table.Column<bool>(type: "boolean", nullable: false),
                    test_fixture_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_fully_implemented = table.Column<bool>(type: "boolean", nullable: false),
                    implementation_notes = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<Guid>(type: "uuid", nullable: true),
                    last_modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gap_rules", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_compliance_gaps_rule",
                schema: "gap",
                table: "compliance_gaps",
                column: "gap_rule_id");

            migrationBuilder.CreateIndex(
                name: "ix_gap_rules_code",
                schema: "gap",
                table: "gap_rules",
                column: "rule_code");

            migrationBuilder.CreateIndex(
                name: "ix_gap_rules_tenant_code",
                schema: "gap",
                table: "gap_rules",
                columns: new[] { "tenant_id", "rule_code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gap_rules",
                schema: "gap");

            migrationBuilder.DropIndex(
                name: "ix_compliance_gaps_rule",
                schema: "gap",
                table: "compliance_gaps");

            migrationBuilder.DropColumn(
                name: "gap_rule_id",
                schema: "gap",
                table: "compliance_gaps");
        }
    }
}
