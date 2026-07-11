using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Evidata.Modules.Audit.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FormalizAuditEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Details",
                schema: "audit",
                table: "audit_logs",
                newName: "Metadata");

            migrationBuilder.RenameColumn(
                name: "Action",
                schema: "audit",
                table: "audit_logs",
                newName: "EventType");

            migrationBuilder.AddColumn<string>(
                name: "CorrelationId",
                schema: "audit",
                table: "audit_logs",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Result",
                schema: "audit",
                table: "audit_logs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_CorrelationId",
                schema: "audit",
                table: "audit_logs",
                column: "CorrelationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_audit_logs_CorrelationId",
                schema: "audit",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "CorrelationId",
                schema: "audit",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "Result",
                schema: "audit",
                table: "audit_logs");

            migrationBuilder.RenameColumn(
                name: "Metadata",
                schema: "audit",
                table: "audit_logs",
                newName: "Details");

            migrationBuilder.RenameColumn(
                name: "EventType",
                schema: "audit",
                table: "audit_logs",
                newName: "Action");
        }
    }
}
