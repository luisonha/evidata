using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Evidata.Modules.Workflow.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewRequirements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "review_requirements",
                schema: "workflow",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    review_type = table.Column<int>(type: "integer", nullable: false),
                    entity_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_required = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    modified_by = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_review_requirements", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_review_requirements_tenant",
                schema: "workflow",
                table: "review_requirements",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_review_requirements_tenant_entity_type",
                schema: "workflow",
                table: "review_requirements",
                columns: new[] { "tenant_id", "entity_type", "review_type" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "review_requirements",
                schema: "workflow");
        }
    }
}
