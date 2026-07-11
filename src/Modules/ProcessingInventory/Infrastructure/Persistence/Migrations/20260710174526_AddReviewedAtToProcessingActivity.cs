using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Evidata.Modules.ProcessingInventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewedAtToProcessingActivity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ActiveVersionId",
                schema: "rat",
                table: "processing_activities",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "reviewed_at",
                schema: "rat",
                table: "processing_activities",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "processing_activity_snapshots",
                schema: "rat",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    activity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    approved_by = table.Column<Guid>(type: "uuid", nullable: false),
                    approved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_processing_activity_snapshots", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_rat_snapshots_activity_version",
                schema: "rat",
                table: "processing_activity_snapshots",
                columns: new[] { "activity_id", "version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "processing_activity_snapshots",
                schema: "rat");

            migrationBuilder.DropColumn(
                name: "ActiveVersionId",
                schema: "rat",
                table: "processing_activities");

            migrationBuilder.DropColumn(
                name: "reviewed_at",
                schema: "rat",
                table: "processing_activities");
        }
    }
}
