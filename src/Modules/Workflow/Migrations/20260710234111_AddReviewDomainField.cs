using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Evidata.Modules.Workflow.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewDomainField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "review_domain",
                schema: "workflow",
                table: "reviews",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "review_domain",
                schema: "workflow",
                table: "reviews");
        }
    }
}
