using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Evidata.Modules.LegalKnowledge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLegalSources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "legal_sources",
                schema: "legal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    jurisdiction = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    published_at = table.Column<DateOnly>(type: "date", nullable: false),
                    official_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_legal_sources", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "legal_source_versions",
                schema: "legal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    legal_source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_number = table.Column<int>(type: "integer", nullable: false),
                    version_tag = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    effective_date = table.Column<DateOnly>(type: "date", nullable: false),
                    summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    changelog_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_legal_source_versions", x => x.id);
                    table.ForeignKey(
                        name: "FK_legal_source_versions_legal_sources_legal_source_id",
                        column: x => x.legal_source_id,
                        principalSchema: "legal",
                        principalTable: "legal_sources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_legal_source_versions_effective_date",
                schema: "legal",
                table: "legal_source_versions",
                column: "effective_date");

            migrationBuilder.CreateIndex(
                name: "ix_legal_source_versions_source_version",
                schema: "legal",
                table: "legal_source_versions",
                columns: new[] { "legal_source_id", "version_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_legal_sources_code",
                schema: "legal",
                table: "legal_sources",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_legal_sources_is_active",
                schema: "legal",
                table: "legal_sources",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_legal_sources_jurisdiction",
                schema: "legal",
                table: "legal_sources",
                column: "jurisdiction");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "legal_source_versions",
                schema: "legal");

            migrationBuilder.DropTable(
                name: "legal_sources",
                schema: "legal");
        }
    }
}
