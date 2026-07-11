using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Evidata.Modules.GapManagement.Migrations
{
    /// <inheritdoc />
    public partial class SeedGapRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "gap",
                table: "gap_rules",
                columns: new[] { "id", "blocks_approval", "created_at", "created_by", "description", "implementation_notes", "is_fully_implemented", "last_modified_at", "last_modified_by", "rule_code", "severity", "tenant_id", "test_fixture_name" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000000"), true, new DateTimeOffset(new DateTime(2026, 7, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("00000000-0000-0000-0000-000000000010"), "Transferencia de datos sin país destino especificado", "Evalúa si un nodo de transferencia internacional tiene especificado el país destino.", true, null, null, "TRANSFER_WITHOUT_DESTINATION_COUNTRY", "Critical", new Guid("00000000-0000-0000-0000-000000000001"), "pa-transfer-no-country" },
                    { new Guid("10000000-0000-0000-0000-000000000001"), true, new DateTimeOffset(new DateTime(2026, 7, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("00000000-0000-0000-0000-000000000010"), "Transferencia sin receptor identificado", "Verifica que todo nodo de transferencia identifique un receptor o destinatario.", true, null, null, "TRANSFER_WITHOUT_RECEIVER", "High", new Guid("00000000-0000-0000-0000-000000000001"), "pa-transfer-no-receiver" },
                    { new Guid("10000000-0000-0000-0000-000000000002"), true, new DateTimeOffset(new DateTime(2026, 7, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("00000000-0000-0000-0000-000000000010"), "Transferencia internacional sin salvaguarda especificada", "Controla que transferencias internacionales de terceros países cuenten con salvaguarda (Capítulo V RGPD).", true, null, null, "TRANSFER_WITHOUT_SAFEGUARD", "Critical", new Guid("00000000-0000-0000-0000-000000000001"), "pa-transfer-no-safeguard" },
                    { new Guid("10000000-0000-0000-0000-000000000003"), true, new DateTimeOffset(new DateTime(2026, 7, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("00000000-0000-0000-0000-000000000010"), "Transferencia sin evidencia de aprobación o base legal", "Valida que exista evidencia adjunta que apruebe o justifique la transferencia internacional.", true, null, null, "TRANSFER_WITHOUT_BLOCKING_EVIDENCE", "Critical", new Guid("00000000-0000-0000-0000-000000000001"), "pa-transfer-no-evidence" },
                    { new Guid("10000000-0000-0000-0000-000000000004"), true, new DateTimeOffset(new DateTime(2026, 7, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("00000000-0000-0000-0000-000000000010"), "Dato sensible sin revisión de seguridad", "Verifica que categorías de datos sensibles tengan validación de evidencia de tipo Security.", true, null, null, "SENSITIVE_DATA_WITHOUT_SECURITY_REVIEW", "Critical", new Guid("00000000-0000-0000-0000-000000000001"), "pa-sensitive-no-security-review" },
                    { new Guid("10000000-0000-0000-0000-000000000005"), true, new DateTimeOffset(new DateTime(2026, 7, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("00000000-0000-0000-0000-000000000010"), "Base legal no especificada", "Comprueba que todo tratamiento de datos cuente con base legal explícita (consentimiento, contrato, obligación legal, etc.).", true, null, null, "LEGAL_BASIS_MISSING", "Critical", new Guid("00000000-0000-0000-0000-000000000001"), "pa-no-legal-basis" },
                    { new Guid("10000000-0000-0000-0000-000000000006"), true, new DateTimeOffset(new DateTime(2026, 7, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("00000000-0000-0000-0000-000000000010"), "Período de retención no definido", "Requiere especificación de período de retención. Nota: El dominio aún no modela explícitamente 'retention_period_days' en ProcessingActivity.", false, null, null, "RETENTION_UNDEFINED", "High", new Guid("00000000-0000-0000-0000-000000000001"), "pa-retention-undefined" },
                    { new Guid("10000000-0000-0000-0000-000000000007"), true, new DateTimeOffset(new DateTime(2026, 7, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("00000000-0000-0000-0000-000000000010"), "Sin categorías de datos especificadas", "Verifica que al menos una categoría de dato esté vinculada al tratamiento.", true, null, null, "DATA_CATEGORIES_EMPTY", "High", new Guid("00000000-0000-0000-0000-000000000001"), "pa-no-data-categories" },
                    { new Guid("10000000-0000-0000-0000-000000000008"), true, new DateTimeOffset(new DateTime(2026, 7, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("00000000-0000-0000-0000-000000000010"), "Propósito del tratamiento no definido", "Asegura que se especifique claramente el propósito o finalidad del tratamiento de datos.", true, null, null, "PURPOSE_UNDEFINED", "Critical", new Guid("00000000-0000-0000-0000-000000000001"), "pa-purpose-undefined" },
                    { new Guid("10000000-0000-0000-0000-000000000009"), true, new DateTimeOffset(new DateTime(2026, 7, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("00000000-0000-0000-0000-000000000010"), "Sin categorías de interesados especificadas", "Comprueba que al menos una categoría de interesado esté identificada en el tratamiento.", true, null, null, "DATA_SUBJECTS_EMPTY", "High", new Guid("00000000-0000-0000-0000-000000000001"), "pa-no-data-subjects" },
                    { new Guid("10000000-0000-0000-0000-000000000010"), true, new DateTimeOffset(new DateTime(2026, 7, 11, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("00000000-0000-0000-0000-000000000010"), "Sistemas sin propietario identificado", "Verifica que todo sistema participante en el tratamiento tenga propietario asignado. Nota: Requiere introspección de nodos que el dominio aún no modeliza completamente.", false, null, null, "SYSTEMS_WITHOUT_OWNER", "High", new Guid("00000000-0000-0000-0000-000000000001"), "pa-systems-without-owner" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "gap",
                table: "gap_rules",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000000"));

            migrationBuilder.DeleteData(
                schema: "gap",
                table: "gap_rules",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                schema: "gap",
                table: "gap_rules",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                schema: "gap",
                table: "gap_rules",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                schema: "gap",
                table: "gap_rules",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                schema: "gap",
                table: "gap_rules",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"));

            migrationBuilder.DeleteData(
                schema: "gap",
                table: "gap_rules",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"));

            migrationBuilder.DeleteData(
                schema: "gap",
                table: "gap_rules",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000007"));

            migrationBuilder.DeleteData(
                schema: "gap",
                table: "gap_rules",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000008"));

            migrationBuilder.DeleteData(
                schema: "gap",
                table: "gap_rules",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000009"));

            migrationBuilder.DeleteData(
                schema: "gap",
                table: "gap_rules",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000010"));
        }
    }
}
