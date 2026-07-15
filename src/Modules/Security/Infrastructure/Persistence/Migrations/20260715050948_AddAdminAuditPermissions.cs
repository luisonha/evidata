using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Evidata.Modules.Security.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminAuditPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "security",
                table: "permissions",
                columns: new[] { "Id", "Action", "Description", "Name", "Resource" },
                values: new object[,]
                {
                    { new Guid("00000001-0000-0000-0000-000000000007"), "read", "Read users in tenant", "Admin.ReadUsers", "admin" },
                    { new Guid("00000001-0000-0000-0000-000000000008"), "manage", "Invite, edit, suspend, reactivate, revoke users in tenant", "Admin.ManageUsers", "admin" },
                    { new Guid("00000001-0000-0000-0000-000000000009"), "readAudit", "Read audit logs in tenant", "Admin.ReadAudit", "admin" },
                    { new Guid("00000001-0000-0000-0000-000000000010"), "changeRole", "Change user role in tenant", "Admin.ChangeUserRole", "admin" }
                });

            migrationBuilder.InsertData(
                schema: "security",
                table: "role_permissions",
                columns: new[] { "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { new Guid("00000001-0000-0000-0000-000000000007"), new Guid("00000000-0000-0000-0000-000000000001") },
                    { new Guid("00000001-0000-0000-0000-000000000008"), new Guid("00000000-0000-0000-0000-000000000001") },
                    { new Guid("00000001-0000-0000-0000-000000000009"), new Guid("00000000-0000-0000-0000-000000000001") },
                    { new Guid("00000001-0000-0000-0000-000000000010"), new Guid("00000000-0000-0000-0000-000000000001") },
                    { new Guid("00000001-0000-0000-0000-000000000007"), new Guid("00000000-0000-0000-0000-000000000002") },
                    { new Guid("00000001-0000-0000-0000-000000000008"), new Guid("00000000-0000-0000-0000-000000000002") },
                    { new Guid("00000001-0000-0000-0000-000000000009"), new Guid("00000000-0000-0000-0000-000000000002") },
                    { new Guid("00000001-0000-0000-0000-000000000010"), new Guid("00000000-0000-0000-0000-000000000002") },
                    { new Guid("00000001-0000-0000-0000-000000000009"), new Guid("00000000-0000-0000-0000-000000000006") }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "security",
                table: "role_permissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000001-0000-0000-0000-000000000007"), new Guid("00000000-0000-0000-0000-000000000001") });

            migrationBuilder.DeleteData(
                schema: "security",
                table: "role_permissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000001-0000-0000-0000-000000000008"), new Guid("00000000-0000-0000-0000-000000000001") });

            migrationBuilder.DeleteData(
                schema: "security",
                table: "role_permissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000001-0000-0000-0000-000000000009"), new Guid("00000000-0000-0000-0000-000000000001") });

            migrationBuilder.DeleteData(
                schema: "security",
                table: "role_permissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000001-0000-0000-0000-000000000010"), new Guid("00000000-0000-0000-0000-000000000001") });

            migrationBuilder.DeleteData(
                schema: "security",
                table: "role_permissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000001-0000-0000-0000-000000000007"), new Guid("00000000-0000-0000-0000-000000000002") });

            migrationBuilder.DeleteData(
                schema: "security",
                table: "role_permissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000001-0000-0000-0000-000000000008"), new Guid("00000000-0000-0000-0000-000000000002") });

            migrationBuilder.DeleteData(
                schema: "security",
                table: "role_permissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000001-0000-0000-0000-000000000009"), new Guid("00000000-0000-0000-0000-000000000002") });

            migrationBuilder.DeleteData(
                schema: "security",
                table: "role_permissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000001-0000-0000-0000-000000000010"), new Guid("00000000-0000-0000-0000-000000000002") });

            migrationBuilder.DeleteData(
                schema: "security",
                table: "role_permissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000001-0000-0000-0000-000000000009"), new Guid("00000000-0000-0000-0000-000000000006") });

            migrationBuilder.DeleteData(
                schema: "security",
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("00000001-0000-0000-0000-000000000007"));

            migrationBuilder.DeleteData(
                schema: "security",
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("00000001-0000-0000-0000-000000000008"));

            migrationBuilder.DeleteData(
                schema: "security",
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("00000001-0000-0000-0000-000000000009"));

            migrationBuilder.DeleteData(
                schema: "security",
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("00000001-0000-0000-0000-000000000010"));
        }
    }
}
