using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Evidata.Modules.ProcessingInventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRatSections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "data_categories",
                schema: "rat",
                table: "processing_activities",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "data_subjects",
                schema: "rat",
                table: "processing_activities",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "legal_basis",
                schema: "rat",
                table: "processing_activities",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "legal_basis_justification",
                schema: "rat",
                table: "processing_activities",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "purpose_text",
                schema: "rat",
                table: "processing_activities",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "data_categories",
                schema: "rat",
                table: "processing_activities");

            migrationBuilder.DropColumn(
                name: "data_subjects",
                schema: "rat",
                table: "processing_activities");

            migrationBuilder.DropColumn(
                name: "legal_basis",
                schema: "rat",
                table: "processing_activities");

            migrationBuilder.DropColumn(
                name: "legal_basis_justification",
                schema: "rat",
                table: "processing_activities");

            migrationBuilder.DropColumn(
                name: "purpose_text",
                schema: "rat",
                table: "processing_activities");
        }
    }
}
