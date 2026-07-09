namespace Evidata.Functions.Notifications.Email;

/// <summary>
/// Plantillas HTML para notificaciones de Evidata.
/// Diseño minimalista sin dependencias externas.
/// </summary>
public static class EmailTemplates
{
    private const string BaseStyle = @"
        font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
        max-width: 600px; margin: 0 auto; padding: 20px;
        color: #1a1a2e; background: #f8f9fa;";

    private const string CardStyle = @"
        background: white; border-radius: 8px; padding: 24px;
        border-left: 4px solid {COLOR}; margin-top: 16px;
        box-shadow: 0 1px 3px rgba(0,0,0,0.1);";

    private const string FooterStyle = @"
        font-size: 12px; color: #6c757d; margin-top: 24px;
        padding-top: 16px; border-top: 1px solid #dee2e6;";

    // ── Brechas de cumplimiento ───────────────────────────────────────────────

    public static string GapCreated(string gapTitle, string severity, string tenantName, string? ownerName) =>
        Build(
            icon: "📋",
            color: SeverityColor(severity),
            title: "Nueva brecha de cumplimiento detectada",
            body: $@"
                <p>Se ha registrado una nueva brecha en <strong>{tenantName}</strong>.</p>
                <table style='width:100%; border-collapse:collapse; margin-top:12px;'>
                    <tr><td style='padding:6px 0; color:#6c757d;'>Brecha:</td>
                        <td><strong>{Escape(gapTitle)}</strong></td></tr>
                    <tr><td style='padding:6px 0; color:#6c757d;'>Severidad:</td>
                        <td><span style='color:{SeverityColor(severity)};font-weight:600;'>{severity}</span></td></tr>
                    {(ownerName != null ? $"<tr><td style='padding:6px 0; color:#6c757d;'>Responsable:</td><td>{Escape(ownerName)}</td></tr>" : "")}
                </table>
                <p style='margin-top:16px;'>Accede a Evidata para revisar los detalles y asignar un plan de remediación.</p>",
            severity: severity);

    public static string GapAssigned(string gapTitle, string severity, string tenantName, string ownerName) =>
        Build(
            icon: "👤",
            color: "#0d6efd",
            title: "Se te ha asignado una brecha de cumplimiento",
            body: $@"
                <p>Hola <strong>{Escape(ownerName)}</strong>,</p>
                <p>Has sido designado responsable de la siguiente brecha en <strong>{tenantName}</strong>:</p>
                <table style='width:100%; border-collapse:collapse; margin-top:12px;'>
                    <tr><td style='padding:6px 0; color:#6c757d;'>Brecha:</td>
                        <td><strong>{Escape(gapTitle)}</strong></td></tr>
                    <tr><td style='padding:6px 0; color:#6c757d;'>Severidad:</td>
                        <td><span style='color:{SeverityColor(severity)};font-weight:600;'>{severity}</span></td></tr>
                </table>
                <p style='margin-top:16px;'>Accede a Evidata para comenzar el plan de remediación.</p>",
            severity: severity);

    public static string GapBlocked(string gapTitle, string reason, string tenantName) =>
        Build(
            icon: "🚧",
            color: "#dc3545",
            title: "Brecha de cumplimiento bloqueada",
            body: $@"
                <p>Una brecha en <strong>{tenantName}</strong> ha sido marcada como <strong>Bloqueada</strong>.</p>
                <table style='width:100%; border-collapse:collapse; margin-top:12px;'>
                    <tr><td style='padding:6px 0; color:#6c757d;'>Brecha:</td>
                        <td><strong>{Escape(gapTitle)}</strong></td></tr>
                    <tr><td style='padding:6px 0; color:#6c757d;'>Motivo:</td>
                        <td>{Escape(reason ?? "Sin detalle especificado")}</td></tr>
                </table>
                <p style='margin-top:16px;color:#dc3545;'>⚠️ Se requiere intervención para desbloquear la remediación.</p>",
            severity: "High");

    public static string GapResolved(string gapTitle, string tenantName) =>
        Build(
            icon: "✅",
            color: "#198754",
            title: "Brecha de cumplimiento resuelta",
            body: $@"
                <p>La siguiente brecha en <strong>{tenantName}</strong> ha sido marcada como <strong>Resuelta</strong>.</p>
                <p style='margin-top:12px;'><strong>{Escape(gapTitle)}</strong></p>
                <p style='margin-top:16px; color:#198754;'>✅ La remediación ha sido completada exitosamente.</p>",
            severity: "Low");

    // ── Actividades de Tratamiento (RAT) ─────────────────────────────────────

    public static string RatSubmittedForReview(string ratName, string tenantName, string submittedBy) =>
        Build(
            icon: "🔍",
            color: "#0dcaf0",
            title: "RAT enviado para revisión",
            body: $@"
                <p>Un Registro de Actividad de Tratamiento ha sido enviado para tu revisión en <strong>{tenantName}</strong>.</p>
                <table style='width:100%; border-collapse:collapse; margin-top:12px;'>
                    <tr><td style='padding:6px 0; color:#6c757d;'>RAT:</td>
                        <td><strong>{Escape(ratName)}</strong></td></tr>
                    <tr><td style='padding:6px 0; color:#6c757d;'>Enviado por:</td>
                        <td>{Escape(submittedBy)}</td></tr>
                </table>
                <p style='margin-top:16px;'>Accede a Evidata para revisar y aprobar o rechazar este tratamiento.</p>",
            severity: "Medium");

    public static string RatApproved(string ratName, string tenantName, string approvedBy) =>
        Build(
            icon: "✅",
            color: "#198754",
            title: "RAT aprobado",
            body: $@"
                <p>Tu Registro de Actividad de Tratamiento ha sido <strong>Aprobado</strong> en <strong>{tenantName}</strong>.</p>
                <table style='width:100%; border-collapse:collapse; margin-top:12px;'>
                    <tr><td style='padding:6px 0; color:#6c757d;'>RAT:</td>
                        <td><strong>{Escape(ratName)}</strong></td></tr>
                    <tr><td style='padding:6px 0; color:#6c757d;'>Aprobado por:</td>
                        <td>{Escape(approvedBy)}</td></tr>
                </table>
                <p style='margin-top:16px; color:#198754;'>El tratamiento está ahora activo y registrado conforme a la Ley 21.719.</p>",
            severity: "Low");

    public static string RatRejected(string ratName, string tenantName, string rejectedBy, string reason) =>
        Build(
            icon: "❌",
            color: "#dc3545",
            title: "RAT rechazado — requiere correcciones",
            body: $@"
                <p>Tu Registro de Actividad de Tratamiento requiere correcciones en <strong>{tenantName}</strong>.</p>
                <table style='width:100%; border-collapse:collapse; margin-top:12px;'>
                    <tr><td style='padding:6px 0; color:#6c757d;'>RAT:</td>
                        <td><strong>{Escape(ratName)}</strong></td></tr>
                    <tr><td style='padding:6px 0; color:#6c757d;'>Rechazado por:</td>
                        <td>{Escape(rejectedBy)}</td></tr>
                    <tr><td style='padding:6px 0; color:#6c757d;'>Motivo:</td>
                        <td style='color:#dc3545;'>{Escape(reason)}</td></tr>
                </table>
                <p style='margin-top:16px;'>Por favor revisa el feedback y vuelve a enviar para aprobación.</p>",
            severity: "High");

    // ── Builder ───────────────────────────────────────────────────────────────

    private static string Build(string icon, string color, string title, string body, string severity) =>
        $@"<!DOCTYPE html><html><head><meta charset='utf-8'>
        <title>{Escape(title)}</title></head>
        <body style='{BaseStyle}'>
          <div style='{CardStyle.Replace("{COLOR}", color)}'>
            <h2 style='margin:0 0 8px; font-size:18px;'>{icon} {Escape(title)}</h2>
            {body}
          </div>
          <div style='{FooterStyle}'>
            Este mensaje fue generado automáticamente por Evidata — Plataforma de Cumplimiento Ley 21.719.
            No responder a este correo.
          </div>
        </body></html>";

    private static string SeverityColor(string severity) => severity switch
    {
        "Critical" => "#7c0000",
        "High"     => "#dc3545",
        "Medium"   => "#fd7e14",
        "Low"      => "#198754",
        _          => "#6c757d"
    };

    private static string Escape(string? s) =>
        System.Net.WebUtility.HtmlEncode(s ?? string.Empty);
}
