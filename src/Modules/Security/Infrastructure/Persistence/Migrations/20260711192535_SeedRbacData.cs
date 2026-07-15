using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Evidata.Modules.Security.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedRbacData : Migration
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
                    { new Guid("00000001-0000-0000-0000-000000000001"), "approve", "Approve a processing activity for review", "processingActivity:approve", "processingActivity" },
                    { new Guid("00000001-0000-0000-0000-000000000002"), "activate", "Activate an approved processing activity", "processingActivity:activate", "processingActivity" },
                    { new Guid("00000001-0000-0000-0000-000000000003"), "validate", "Validate evidence requirements", "evidence:validate", "evidence" },
                    { new Guid("00000001-0000-0000-0000-000000000004"), "acceptWithRisk", "Accept gaps with risk justification", "gap:acceptWithRisk", "gap" },
                    { new Guid("00000001-0000-0000-0000-000000000005"), "generate", "Generate official exports", "export:generate", "export" },
                    { new Guid("00000001-0000-0000-0000-000000000006"), "download", "Download evidence files", "evidence:download", "evidence" },
                    { new Guid("00000001-0000-0000-0000-000000000007"), "read", "Read users in tenant", "Admin.ReadUsers", "admin" },
                    { new Guid("00000001-0000-0000-0000-000000000008"), "manage", "Invite, edit, suspend, reactivate, revoke users in tenant", "Admin.ManageUsers", "admin" },
                    { new Guid("00000001-0000-0000-0000-000000000009"), "readAudit", "Read audit logs in tenant", "Admin.ReadAudit", "admin" },
                    { new Guid("00000001-0000-0000-0000-000000000010"), "changeRole", "Change user role in tenant", "Admin.ChangeUserRole", "admin" }
                });

            migrationBuilder.InsertData(
                schema: "security",
                table: "roles",
                columns: new[] { "Id", "Description", "IsSystemRole", "Name" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000001"), "Tenant owner - highest privilege", true, "TenantOwner" },
                    { new Guid("00000000-0000-0000-0000-000000000002"), "Compliance administrator", true, "ComplianceAdmin" },
                    { new Guid("00000000-0000-0000-0000-000000000003"), "Process owner - manages individual processing activities", true, "ProcessOwner" },
                    { new Guid("00000000-0000-0000-0000-000000000004"), "Legal domain reviewer for evidence validation", true, "LegalReviewer" },
                    { new Guid("00000000-0000-0000-0000-000000000005"), "Security domain reviewer for evidence validation", true, "SecurityReviewer" },
                    { new Guid("00000000-0000-0000-0000-000000000006"), "Auditor - read-only access with audit rights", true, "Auditor" },
                    { new Guid("00000000-0000-0000-0000-000000000007"), "Viewer - read-only access to published information", true, "Viewer" }
                });

            migrationBuilder.InsertData(
                schema: "security",
                table: "role_permissions",
                columns: new[] { "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { new Guid("00000001-0000-0000-0000-000000000001"), new Guid("00000000-0000-0000-0000-000000000001") },
                    { new Guid("00000001-0000-0000-0000-000000000002"), new Guid("00000000-0000-0000-0000-000000000001") },
                    { new Guid("00000001-0000-0000-0000-000000000003"), new Guid("00000000-0000-0000-0000-000000000001") },
                    { new Guid("00000001-0000-0000-0000-000000000004"), new Guid("00000000-0000-0000-0000-000000000001") },
                    { new Guid("00000001-0000-0000-0000-000000000005"), new Guid("00000000-0000-0000-0000-000000000001") },
                    { new Guid("00000001-0000-0000-0000-000000000006"), new Guid("00000000-0000-0000-0000-000000000001") },
                    { new Guid("00000001-0000-0000-0000-000000000007"), new Guid("00000000-0000-0000-0000-000000000001") },
                    { new Guid("00000001-0000-0000-0000-000000000008"), new Guid("00000000-0000-0000-0000-000000000001") },
                    { new Guid("00000001-0000-0000-0000-000000000009"), new Guid("00000000-0000-0000-0000-000000000001") },
                    { new Guid("00000001-0000-0000-0000-000000000010"), new Guid("00000000-0000-0000-0000-000000000001") },
                    { new Guid("00000001-0000-0000-0000-000000000001"), new Guid("00000000-0000-0000-0000-000000000002") },
                    { new Guid("00000001-0000-0000-0000-000000000002"), new Guid("00000000-0000-0000-0000-000000000002") },
                    { new Guid("00000001-0000-0000-0000-000000000003"), new Guid("00000000-0000-0000-0000-000000000002") },
                    { new Guid("00000001-0000-0000-0000-000000000004"), new Guid("00000000-0000-0000-0000-000000000002") },
                    { new Guid("00000001-0000-0000-0000-000000000005"), new Guid("00000000-0000-0000-0000-000000000002") },
                    { new Guid("00000001-0000-0000-0000-000000000006"), new Guid("00000000-0000-0000-0000-000000000002") },
                    { new Guid("00000001-0000-0000-0000-000000000007"), new Guid("00000000-0000-0000-0000-000000000002") },
                    { new Guid("00000001-0000-0000-0000-000000000008"), new Guid("00000000-0000-0000-0000-000000000002") },
                    { new Guid("00000001-0000-0000-0000-000000000009"), new Guid("00000000-0000-0000-0000-000000000002") },
                    { new Guid("00000001-0000-0000-0000-000000000010"), new Guid("00000000-0000-0000-0000-000000000002") },
                    { new Guid("00000001-0000-0000-0000-000000000001"), new Guid("00000000-0000-0000-0000-000000000003") },
                    { new Guid("00000001-0000-0000-0000-000000000003"), new Guid("00000000-0000-0000-0000-000000000003") },
                    { new Guid("00000001-0000-0000-0000-000000000005"), new Guid("00000000-0000-0000-0000-000000000003") },
                    { new Guid("00000001-0000-0000-0000-000000000006"), new Guid("00000000-0000-0000-0000-000000000003") },
                    { new Guid("00000001-0000-0000-0000-000000000003"), new Guid("00000000-0000-0000-0000-000000000004") },
                    { new Guid("00000001-0000-0000-0000-000000000006"), new Guid("00000000-0000-0000-0000-000000000004") },
                    { new Guid("00000001-0000-0000-0000-000000000003"), new Guid("00000000-0000-0000-0000-000000000005") },
                    { new Guid("00000001-0000-0000-0000-000000000006"), new Guid("00000000-0000-0000-0000-000000000005") },
                    { new Guid("00000001-0000-0000-0000-000000000003"), new Guid("00000000-0000-0000-0000-000000000006") },
                    { new Guid("00000001-0000-0000-0000-000000000006"), new Guid("00000000-0000-0000-0000-000000000006") },
                    { new Guid("00000001-0000-0000-0000-000000000009"), new Guid("00000000-0000-0000-0000-000000000006") },
                    { new Guid("00000001-0000-0000-0000-000000000006"), new Guid("00000000-0000-0000-0000-000000000007") }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "security",
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("00000001-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                schema: "security",
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("00000001-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                schema: "security",
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("00000001-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                schema: "security",
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("00000001-0000-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                schema: "security",
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("00000001-0000-0000-0000-000000000005"));

            migrationBuilder.DeleteData(
                schema: "security",
                table: "permissions",
                keyColumn: "Id",
                keyValue: new Guid("00000001-0000-0000-0000-000000000006"));

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

            migrationBuilder.DeleteData(
                schema: "security",
                table: "role_permissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000001-0000-0000-0000-000000000001"), new Guid("00000000-0000-0000-0000-000000000001") });

            migrationBuilder.DeleteData(
                schema: "security",
                table: "role_permissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000001-0000-0000-0000-000000000002"), new Guid("00000000-0000-0000-0000-000000000001") });

            migrationBuilder.DeleteData(
                schema: "security",
                table: "role_permissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000001-0000-0000-0000-000000000003"), new Guid("00000000-0000-0000-0000-000000000001") });

            migrationBuilder.DeleteData(
                schema: "security",
                table: "role_permissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000001-0000-0000-0000-000000000004"), new Guid("00000000-0000-0000-0000-000000000001") });

            migrationBuilder.DeleteData(
                schema: "security",
                table: "role_permissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000001-0000-0000-0000-000000000005"), new Guid("00000000-0000-0000-0000-000000000001") });

            migrationBuilder.DeleteData(
                schema: "security",
                table: "role_permissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000001-0000-0000-0000-000000000006"), new Guid("00000000-0000-0000-0000-000000000001") });

            migrationBuilder.DeleteData(
                schema: "security",
                table: "role_permissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000001-0000-0000-0000-000000000001"), new Guid("00000000-0000-0000-0000-000000000002") });

            migrationBuilder.DeleteData(
                schema: "security",
                table: "role_permissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000001-0000-0000-0000-000000000002"), new Guid("00000000-0000-0000-0000-000000000002") });

            migrationBuilder.DeleteData(
                schema: "security",
                table: "role_permissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000001-0000-0000-0000-000000000003"), new Guid("00000000-0000-0000-0000-000000000002") });

            migrationBuilder.DeleteData(
                schema: "security",
                table: "role_permissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000001-0000-0000-0000-000000000004"), new Guid("00000000-0000-0000-0000-000000000002") });

            migrationBuilder.DeleteData(
                schema: "security",
                table: "role_permissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000001-0000-0000-0000-000000000005"), new Guid("00000000-0000-0000-0000-000000000002") });

            migrationBuilder.DeleteData(
                schema: "security",
                table: "role_permissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000001-0000-0000-0000-000000000006"), new Guid("00000000-0000-0000-0000-000000000002") });

            migrationBuilder.DeleteData(
                schema: "security",
                table: "role_permissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000001-0000-0000-0000-000000000001"), new Guid("00000000-0000-0000-0000-000000000003") });

            migrationBuilder.DeleteData(
                schema: "security",
                table: "role_permissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000001-0000-0000-0000-000000000003"), new Guid("00000000-0000-0000-0000-000000000003") });

            migrationBuilder.DeleteData(
                schema: "security",
                table: "role_permissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000001-0000-0000-0000-000000000005"), new Guid("00000000-0000-0000-0000-000000000003") });

            migrationBuilder.DeleteData(
                schema: "security",
                table: "role_permissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000001-0000-0000-0000-000000000006"), new Guid("00000000-0000-0000-0000-000000000003") });

            migrationBuilder.DeleteData(
                schema: "security",
                table: "role_permissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000001-0000-0000-0000-000000000003"), new Guid("00000000-0000-0000-0000-000000000004") });

            migrationBuilder.DeleteData(
                schema: "security",
                table: "role_permissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000001-0000-0000-0000-000000000006"), new Guid("00000000-0000-0000-0000-000000000004") });

            migrationBuilder.DeleteData(
                schema: "security",
                table: "role_permissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000001-0000-0000-0000-000000000003"), new Guid("00000000-0000-0000-0000-000000000005") });

            migrationBuilder.DeleteData(
                schema: "security",
                table: "role_permissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000001-0000-0000-0000-000000000006"), new Guid("00000000-0000-0000-0000-000000000005") });

            migrationBuilder.DeleteData(
                schema: "security",
                table: "role_permissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000001-0000-0000-0000-000000000003"), new Guid("00000000-0000-0000-0000-000000000006") });

            migrationBuilder.DeleteData(
                schema: "security",
                table: "role_permissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000001-0000-0000-0000-000000000006"), new Guid("00000000-0000-0000-0000-000000000006") });

            migrationBuilder.DeleteData(
                schema: "security",
                table: "role_permissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { new Guid("00000001-0000-0000-0000-000000000006"), new Guid("00000000-0000-0000-0000-000000000007") });

            migrationBuilder.DeleteData(
                schema: "security",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                schema: "security",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                schema: "security",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                schema: "security",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                schema: "security",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000005"));

            migrationBuilder.DeleteData(
                schema: "security",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000006"));

            migrationBuilder.DeleteData(
                schema: "security",
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000007"));
        }
    }
}
