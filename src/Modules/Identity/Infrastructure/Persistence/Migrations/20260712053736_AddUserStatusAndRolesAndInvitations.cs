using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Evidata.Modules.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserStatusAndRolesAndInvitations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "identity");

            migrationBuilder.CreateTable(
                name: "invitations",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Roles = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false, comment: "Comma-separated list of role names"),
                    ResponsibleAreaId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invitations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "user_profiles",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Provider = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_profiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "user_profile_roles",
                schema: "identity",
                columns: table => new
                {
                    UserProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_profile_roles", x => new { x.UserProfileId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_user_profile_roles_user_profiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalSchema: "identity",
                        principalTable: "user_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_invitations_Email",
                schema: "identity",
                table: "invitations",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_invitations_ExpiresAt",
                schema: "identity",
                table: "invitations",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_invitations_Status",
                schema: "identity",
                table: "invitations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_invitations_TenantId",
                schema: "identity",
                table: "invitations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_invitations_TenantId_Email",
                schema: "identity",
                table: "invitations",
                columns: new[] { "TenantId", "Email" });

            migrationBuilder.CreateIndex(
                name: "IX_invitations_TenantId_Status",
                schema: "identity",
                table: "invitations",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_invitations_UserId",
                schema: "identity",
                table: "invitations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_user_profile_roles_RoleId",
                schema: "identity",
                table: "user_profile_roles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_user_profile_roles_TenantId",
                schema: "identity",
                table: "user_profile_roles",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_user_profile_roles_TenantId_RoleId",
                schema: "identity",
                table: "user_profile_roles",
                columns: new[] { "TenantId", "RoleId" });

            migrationBuilder.CreateIndex(
                name: "IX_user_profile_roles_TenantId_UserProfileId",
                schema: "identity",
                table: "user_profile_roles",
                columns: new[] { "TenantId", "UserProfileId" });

            migrationBuilder.CreateIndex(
                name: "IX_user_profiles_ExternalId_Provider_TenantId",
                schema: "identity",
                table: "user_profiles",
                columns: new[] { "ExternalId", "Provider", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_profiles_Status",
                schema: "identity",
                table: "user_profiles",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_user_profiles_TenantId",
                schema: "identity",
                table: "user_profiles",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "invitations",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "user_profile_roles",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "user_profiles",
                schema: "identity");
        }
    }
}
