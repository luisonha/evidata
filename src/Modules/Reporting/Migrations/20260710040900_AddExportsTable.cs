using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Evidata.Modules.Reporting.Migrations
{
    /// <inheritdoc />
    public partial class AddExportsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "exports",
                schema: "reporting",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    processing_activity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    export_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    artifact_document_id = table.Column<Guid>(type: "uuid", nullable: true),
                    requested_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    generated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    warnings = table.Column<List<string>>(type: "jsonb[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exports", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_exports_activity_type",
                schema: "reporting",
                table: "exports",
                columns: new[] { "processing_activity_id", "export_type" });

            migrationBuilder.CreateIndex(
                name: "ix_exports_correlation_id",
                schema: "reporting",
                table: "exports",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "ix_exports_tenant_activity",
                schema: "reporting",
                table: "exports",
                columns: new[] { "tenant_id", "processing_activity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_exports_tenant_status",
                schema: "reporting",
                table: "exports",
                columns: new[] { "tenant_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "exports",
                schema: "reporting");
        }
    }
}
