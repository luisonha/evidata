using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Evidata.Modules.ProcessingInventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRatSystemsSections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "retention_legal_justification",
                schema: "rat",
                table: "processing_activities",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "retention_months",
                schema: "rat",
                table: "processing_activities",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "retention_period_description",
                schema: "rat",
                table: "processing_activities",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "security_measures",
                schema: "rat",
                table: "processing_activities",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "suppliers",
                schema: "rat",
                table: "processing_activities",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "systems",
                schema: "rat",
                table: "processing_activities",
                type: "jsonb",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "retention_legal_justification",
                schema: "rat",
                table: "processing_activities");

            migrationBuilder.DropColumn(
                name: "retention_months",
                schema: "rat",
                table: "processing_activities");

            migrationBuilder.DropColumn(
                name: "retention_period_description",
                schema: "rat",
                table: "processing_activities");

            migrationBuilder.DropColumn(
                name: "security_measures",
                schema: "rat",
                table: "processing_activities");

            migrationBuilder.DropColumn(
                name: "suppliers",
                schema: "rat",
                table: "processing_activities");

            migrationBuilder.DropColumn(
                name: "systems",
                schema: "rat",
                table: "processing_activities");
        }
    }
}
