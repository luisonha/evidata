using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Evidata.Modules.Mcp.Migrations
{
    /// <inheritdoc />
    public partial class InitialMcp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "mcp");

            migrationBuilder.CreateTable(
                name: "mcp_interactions",
                schema: "mcp",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    question = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    answer = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    risk_level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    used_tenant_context = table.Column<bool>(type: "boolean", nullable: false),
                    requires_human_review = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mcp_interactions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "mcp_citations",
                schema: "mcp",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    interaction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    source_id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    source_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    fragment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mcp_citations", x => x.id);
                    table.ForeignKey(
                        name: "FK_mcp_citations_mcp_interactions_interaction_id",
                        column: x => x.interaction_id,
                        principalSchema: "mcp",
                        principalTable: "mcp_interactions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_mcp_citations_interaction_id",
                schema: "mcp",
                table: "mcp_citations",
                column: "interaction_id");

            migrationBuilder.CreateIndex(
                name: "ix_mcp_interactions_tenant_status",
                schema: "mcp",
                table: "mcp_interactions",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_mcp_interactions_tenant_time",
                schema: "mcp",
                table: "mcp_interactions",
                columns: new[] { "tenant_id", "occurred_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mcp_citations",
                schema: "mcp");

            migrationBuilder.DropTable(
                name: "mcp_interactions",
                schema: "mcp");
        }
    }
}
