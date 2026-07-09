using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Evidata.Modules.Mcp.Migrations
{
    /// <inheritdoc />
    public partial class AddMcpReviewAndFeedback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "mcp_feedbacks",
                schema: "mcp",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    interaction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rating = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mcp_feedbacks", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "mcp_review_tasks",
                schema: "mcp",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    interaction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_to = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    review_notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mcp_review_tasks", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_mcp_feedbacks_interaction_id",
                schema: "mcp",
                table: "mcp_feedbacks",
                column: "interaction_id");

            migrationBuilder.CreateIndex(
                name: "ix_mcp_feedbacks_tenant_user",
                schema: "mcp",
                table: "mcp_feedbacks",
                columns: new[] { "tenant_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "ix_mcp_review_tasks_interaction_id",
                schema: "mcp",
                table: "mcp_review_tasks",
                column: "interaction_id");

            migrationBuilder.CreateIndex(
                name: "ix_mcp_review_tasks_tenant_status",
                schema: "mcp",
                table: "mcp_review_tasks",
                columns: new[] { "tenant_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mcp_feedbacks",
                schema: "mcp");

            migrationBuilder.DropTable(
                name: "mcp_review_tasks",
                schema: "mcp");
        }
    }
}
