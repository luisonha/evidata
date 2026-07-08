using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Evidata.Modules.LegalKnowledge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialLegalObligations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "legal");

            migrationBuilder.CreateTable(
                name: "legal_obligations",
                schema: "legal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    legal_text = table.Column<string>(type: "text", nullable: false),
                    source_article = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    legal_source_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    deadline_days = table.Column<int>(type: "integer", nullable: true),
                    frequency = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_legal_obligations", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_legal_obligations_code",
                schema: "legal",
                table: "legal_obligations",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_legal_obligations_source",
                schema: "legal",
                table: "legal_obligations",
                column: "legal_source_name");

            migrationBuilder.CreateIndex(
                name: "ix_legal_obligations_status",
                schema: "legal",
                table: "legal_obligations",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "legal_obligations",
                schema: "legal");
        }
    }
}
