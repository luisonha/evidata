using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Evidata.Modules.Workflow.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "workflow_tasks",
                schema: "workflow",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_module = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    target_entity_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    target_entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    assigned_to = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    due_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_tasks", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_workflow_tasks_tenant_assignee",
                schema: "workflow",
                table: "workflow_tasks",
                columns: new[] { "tenant_id", "assigned_to" });

            migrationBuilder.CreateIndex(
                name: "ix_workflow_tasks_tenant_entity",
                schema: "workflow",
                table: "workflow_tasks",
                columns: new[] { "tenant_id", "target_entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_workflow_tasks_tenant_status",
                schema: "workflow",
                table: "workflow_tasks",
                columns: new[] { "tenant_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "workflow_tasks",
                schema: "workflow");
        }
    }
}
