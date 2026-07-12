using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Evidata.Modules.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedLocalDevUsers : Migration
    {
        /// <summary>
        /// DEVELOPMENT ONLY: Seeds 10 local user profiles for dev/test environments.
        /// This migration should NEVER be applied to production databases.
        /// 
        /// The 10 profiles model different user states and roles:
        /// - admin.local: Active TenantOwner (full admin access)
        /// - compliance.local: Active ComplianceAdmin (compliance operations)
        /// - owner.local: Active ProcessOwner (process responsibility)
        /// - reviewer.local: Active LegalReviewer (evidence validation)
        /// - viewer.local: Active Viewer (read-only access)
        /// - invited.local: Invited ProcessOwner (pending invitation acceptance)
        /// - suspended.local: Suspended Viewer (temporarily deactivated)
        /// - disabled.local: Disabled Viewer (permanently deactivated)
        /// - denied.local: Active with no roles (tests authorization denial)
        /// - no-tenant.local: Active TenantOwner but without tenant assignment (tests tenant isolation)
        /// 
        /// These GUIDs are fixed and documented for use in development headers:
        /// X-Evidata-Dev-User: {UserId}
        /// X-Evidata-Dev-Tenant: {TenantId}
        /// </summary>
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Tenant 1 (default): 00000000-0000-0000-0000-000000000001
            // Tenant 2 (for no-tenant scenario): 00000000-0000-0000-0000-000000000002

            // Role GUIDs (from SeedRbacData migration):
            // TenantOwner: 00000000-0000-0000-0000-000000000001
            // ComplianceAdmin: 00000000-0000-0000-0000-000000000002
            // ProcessOwner: 00000000-0000-0000-0000-000000000003
            // LegalReviewer: 00000000-0000-0000-0000-000000000004
            // SecurityReviewer: 00000000-0000-0000-0000-000000000005
            // Auditor: 00000000-0000-0000-0000-000000000006
            // Viewer: 00000000-0000-0000-0000-000000000007

            var now = DateTime.UtcNow;

            // ── Insert user_profiles ──────────────────────────────────────────
            migrationBuilder.InsertData(
                schema: "identity",
                table: "user_profiles",
                columns: new[] { "Id", "ExternalId", "Provider", "Email", "DisplayName", "TenantId", "Status", "CreatedAt", "UpdatedAt" },
                values: new object[,]
                {
                    // admin.local: Active, TenantOwner
                    {
                        new Guid("00000000-0000-0000-0000-000000000020"),
                        "admin.local",
                        "local",
                        "admin@local.evidata",
                        "Admin LocalDev",
                        new Guid("00000000-0000-0000-0000-000000000001"),
                        1, // Active
                        now,
                        now
                    },
                    // compliance.local: Active, ComplianceAdmin
                    {
                        new Guid("00000000-0000-0000-0000-000000000021"),
                        "compliance.local",
                        "local",
                        "compliance@local.evidata",
                        "Compliance LocalDev",
                        new Guid("00000000-0000-0000-0000-000000000001"),
                        1, // Active
                        now,
                        now
                    },
                    // owner.local: Active, ProcessOwner
                    {
                        new Guid("00000000-0000-0000-0000-000000000022"),
                        "owner.local",
                        "local",
                        "owner@local.evidata",
                        "Owner LocalDev",
                        new Guid("00000000-0000-0000-0000-000000000001"),
                        1, // Active
                        now,
                        now
                    },
                    // reviewer.local: Active, LegalReviewer
                    {
                        new Guid("00000000-0000-0000-0000-000000000023"),
                        "reviewer.local",
                        "local",
                        "reviewer@local.evidata",
                        "Reviewer LocalDev",
                        new Guid("00000000-0000-0000-0000-000000000001"),
                        1, // Active
                        now,
                        now
                    },
                    // viewer.local: Active, Viewer
                    {
                        new Guid("00000000-0000-0000-0000-000000000024"),
                        "viewer.local",
                        "local",
                        "viewer@local.evidata",
                        "Viewer LocalDev",
                        new Guid("00000000-0000-0000-0000-000000000001"),
                        1, // Active
                        now,
                        now
                    },
                    // invited.local: Invited, ProcessOwner (pending acceptance)
                    {
                        new Guid("00000000-0000-0000-0000-000000000025"),
                        "invited.local",
                        "local",
                        "invited@local.evidata",
                        "Invited LocalDev",
                        new Guid("00000000-0000-0000-0000-000000000001"),
                        0, // Invited
                        now,
                        now
                    },
                    // suspended.local: Suspended, Viewer
                    {
                        new Guid("00000000-0000-0000-0000-000000000026"),
                        "suspended.local",
                        "local",
                        "suspended@local.evidata",
                        "Suspended LocalDev",
                        new Guid("00000000-0000-0000-0000-000000000001"),
                        2, // Suspended
                        now,
                        now
                    },
                    // disabled.local: Disabled, Viewer
                    {
                        new Guid("00000000-0000-0000-0000-000000000027"),
                        "disabled.local",
                        "local",
                        "disabled@local.evidata",
                        "Disabled LocalDev",
                        new Guid("00000000-0000-0000-0000-000000000001"),
                        3, // Disabled
                        now,
                        now
                    },
                    // denied.local: Active, no roles (tests authorization failure)
                    {
                        new Guid("00000000-0000-0000-0000-000000000028"),
                        "denied.local",
                        "local",
                        "denied@local.evidata",
                        "Denied LocalDev",
                        new Guid("00000000-0000-0000-0000-000000000001"),
                        1, // Active
                        now,
                        now
                    },
                    // no-tenant.local: Active, TenantOwner, but in different tenant (tests tenant isolation)
                    {
                        new Guid("00000000-0000-0000-0000-000000000029"),
                        "no-tenant.local",
                        "local",
                        "no-tenant@local.evidata",
                        "No-Tenant LocalDev",
                        new Guid("00000000-0000-0000-0000-000000000002"),
                        1, // Active
                        now,
                        now
                    }
                });

            // ── Insert user_profile_roles (many-to-many assignments) ──────────
            migrationBuilder.InsertData(
                schema: "identity",
                table: "user_profile_roles",
                columns: new[] { "UserProfileId", "RoleId", "TenantId", "AssignedAt" },
                values: new object[,]
                {
                    // admin.local → TenantOwner
                    {
                        new Guid("00000000-0000-0000-0000-000000000020"),
                        new Guid("00000000-0000-0000-0000-000000000001"), // TenantOwner
                        new Guid("00000000-0000-0000-0000-000000000001"),
                        now
                    },
                    // compliance.local → ComplianceAdmin
                    {
                        new Guid("00000000-0000-0000-0000-000000000021"),
                        new Guid("00000000-0000-0000-0000-000000000002"), // ComplianceAdmin
                        new Guid("00000000-0000-0000-0000-000000000001"),
                        now
                    },
                    // owner.local → ProcessOwner
                    {
                        new Guid("00000000-0000-0000-0000-000000000022"),
                        new Guid("00000000-0000-0000-0000-000000000003"), // ProcessOwner
                        new Guid("00000000-0000-0000-0000-000000000001"),
                        now
                    },
                    // reviewer.local → LegalReviewer
                    {
                        new Guid("00000000-0000-0000-0000-000000000023"),
                        new Guid("00000000-0000-0000-0000-000000000004"), // LegalReviewer
                        new Guid("00000000-0000-0000-0000-000000000001"),
                        now
                    },
                    // viewer.local → Viewer
                    {
                        new Guid("00000000-0000-0000-0000-000000000024"),
                        new Guid("00000000-0000-0000-0000-000000000007"), // Viewer
                        new Guid("00000000-0000-0000-0000-000000000001"),
                        now
                    },
                    // invited.local → ProcessOwner
                    {
                        new Guid("00000000-0000-0000-0000-000000000025"),
                        new Guid("00000000-0000-0000-0000-000000000003"), // ProcessOwner
                        new Guid("00000000-0000-0000-0000-000000000001"),
                        now
                    },
                    // suspended.local → Viewer
                    {
                        new Guid("00000000-0000-0000-0000-000000000026"),
                        new Guid("00000000-0000-0000-0000-000000000007"), // Viewer
                        new Guid("00000000-0000-0000-0000-000000000001"),
                        now
                    },
                    // disabled.local → Viewer
                    {
                        new Guid("00000000-0000-0000-0000-000000000027"),
                        new Guid("00000000-0000-0000-0000-000000000007"), // Viewer
                        new Guid("00000000-0000-0000-0000-000000000001"),
                        now
                    },
                    // denied.local → [no roles assigned]

                    // no-tenant.local → TenantOwner (in different tenant)
                    {
                        new Guid("00000000-0000-0000-0000-000000000029"),
                        new Guid("00000000-0000-0000-0000-000000000001"), // TenantOwner
                        new Guid("00000000-0000-0000-0000-000000000002"),
                        now
                    }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Delete user_profile_roles for the 10 seed users
            var userIds = new[]
            {
                new Guid("00000000-0000-0000-0000-000000000020"), // admin.local
                new Guid("00000000-0000-0000-0000-000000000021"), // compliance.local
                new Guid("00000000-0000-0000-0000-000000000022"), // owner.local
                new Guid("00000000-0000-0000-0000-000000000023"), // reviewer.local
                new Guid("00000000-0000-0000-0000-000000000024"), // viewer.local
                new Guid("00000000-0000-0000-0000-000000000025"), // invited.local
                new Guid("00000000-0000-0000-0000-000000000026"), // suspended.local
                new Guid("00000000-0000-0000-0000-000000000027"), // disabled.local
                new Guid("00000000-0000-0000-0000-000000000028"), // denied.local
                new Guid("00000000-0000-0000-0000-000000000029")  // no-tenant.local
            };

            foreach (var userId in userIds)
            {
                migrationBuilder.DeleteData(
                    schema: "identity",
                    table: "user_profile_roles",
                    keyColumns: new[] { "UserProfileId", "RoleId" },
                    keyValues: new object[] { userId, new Guid("00000000-0000-0000-0000-000000000001") }); // TenantOwner

                migrationBuilder.DeleteData(
                    schema: "identity",
                    table: "user_profile_roles",
                    keyColumns: new[] { "UserProfileId", "RoleId" },
                    keyValues: new object[] { userId, new Guid("00000000-0000-0000-0000-000000000002") }); // ComplianceAdmin

                migrationBuilder.DeleteData(
                    schema: "identity",
                    table: "user_profile_roles",
                    keyColumns: new[] { "UserProfileId", "RoleId" },
                    keyValues: new object[] { userId, new Guid("00000000-0000-0000-0000-000000000003") }); // ProcessOwner

                migrationBuilder.DeleteData(
                    schema: "identity",
                    table: "user_profile_roles",
                    keyColumns: new[] { "UserProfileId", "RoleId" },
                    keyValues: new object[] { userId, new Guid("00000000-0000-0000-0000-000000000004") }); // LegalReviewer

                migrationBuilder.DeleteData(
                    schema: "identity",
                    table: "user_profile_roles",
                    keyColumns: new[] { "UserProfileId", "RoleId" },
                    keyValues: new object[] { userId, new Guid("00000000-0000-0000-0000-000000000007") }); // Viewer
            }

            // Delete user_profiles
            migrationBuilder.DeleteData(
                schema: "identity",
                table: "user_profiles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000020")); // admin.local

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "user_profiles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000021")); // compliance.local

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "user_profiles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000022")); // owner.local

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "user_profiles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000023")); // reviewer.local

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "user_profiles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000024")); // viewer.local

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "user_profiles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000025")); // invited.local

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "user_profiles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000026")); // suspended.local

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "user_profiles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000027")); // disabled.local

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "user_profiles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000028")); // denied.local

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "user_profiles",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000029")); // no-tenant.local
        }
    }
}
