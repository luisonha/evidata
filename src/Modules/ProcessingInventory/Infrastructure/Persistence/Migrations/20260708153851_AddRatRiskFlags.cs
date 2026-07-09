using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Evidata.Modules.ProcessingInventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRatRiskFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "flag_automated_decision",
                schema: "rat",
                table: "processing_activities",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "flag_biometric_data",
                schema: "rat",
                table: "processing_activities",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "flag_children_data",
                schema: "rat",
                table: "processing_activities",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "flag_critical_gap_open",
                schema: "rat",
                table: "processing_activities",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "flag_international_transfer",
                schema: "rat",
                table: "processing_activities",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "flag_missing_legal_basis_evidence",
                schema: "rat",
                table: "processing_activities",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "flag_missing_retention",
                schema: "rat",
                table: "processing_activities",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "flag_missing_security_measures",
                schema: "rat",
                table: "processing_activities",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "flag_sensitive_data",
                schema: "rat",
                table: "processing_activities",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "has_automated_decision",
                schema: "rat",
                table: "processing_activities",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "has_international_transfer",
                schema: "rat",
                table: "processing_activities",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "flag_automated_decision",
                schema: "rat",
                table: "processing_activities");

            migrationBuilder.DropColumn(
                name: "flag_biometric_data",
                schema: "rat",
                table: "processing_activities");

            migrationBuilder.DropColumn(
                name: "flag_children_data",
                schema: "rat",
                table: "processing_activities");

            migrationBuilder.DropColumn(
                name: "flag_critical_gap_open",
                schema: "rat",
                table: "processing_activities");

            migrationBuilder.DropColumn(
                name: "flag_international_transfer",
                schema: "rat",
                table: "processing_activities");

            migrationBuilder.DropColumn(
                name: "flag_missing_legal_basis_evidence",
                schema: "rat",
                table: "processing_activities");

            migrationBuilder.DropColumn(
                name: "flag_missing_retention",
                schema: "rat",
                table: "processing_activities");

            migrationBuilder.DropColumn(
                name: "flag_missing_security_measures",
                schema: "rat",
                table: "processing_activities");

            migrationBuilder.DropColumn(
                name: "flag_sensitive_data",
                schema: "rat",
                table: "processing_activities");

            migrationBuilder.DropColumn(
                name: "has_automated_decision",
                schema: "rat",
                table: "processing_activities");

            migrationBuilder.DropColumn(
                name: "has_international_transfer",
                schema: "rat",
                table: "processing_activities");
        }
    }
}
