# Runbook: Proceso de Revisión Humana HITL (Compliance)

**Versión:** 1.0 | **Actualizado:** 2026-07-08  
**Audiencia:** Equipo de cumplimiento, compliance officers

---

## ¿Qué es HITL?

Human-In-The-Loop (HITL) es el proceso donde el asistente MCP de Evidata eleva una consulta
al equipo humano de cumplimiento porque la respuesta automática fue clasificada como de
**alto riesgo** o porque ocurrió un **error en el modelo**.

El equipo de cumplimiento revisa la respuesta generada y decide si:
- **Aprobar** — la respuesta es correcta y cumple con la Ley 21.719
- **Rechazar** — la respuesta tiene errores o podría inducir al cliente a incumplimiento

---

## Flujo de revisión

```
Consulta usuario → Asistente MCP → RiskLevel = High o Falla
                                        ↓
                              McpReviewTask creada (estado: Open)
                                        ↓
                              Revisor asignado (estado: InProgress)
                                        ↓
                    ┌──────────────────────────────────┐
                    │  Revisor revisa respuesta         │
                    └──────────────────────────────────┘
                           ↓                    ↓
                       Aprobar              Rechazar (con notas)
                           ↓                    ↓
                      Completed             Completed
                   (respuesta válida)   (caso documentado)
```

---

## Acceso al panel de revisión HITL

```
URL: https://app.evidata.cl/compliance/hitl-review
Permisos requeridos: rol "ComplianceReviewer" o "ComplianceAdmin"
```

---

## Proceso de revisión (paso a paso)

### 1. Identificar tareas pendientes

Filtrar por estado **Open** en el panel, ordenadas por `CreatedAt` ascendente (primero las más antiguas).

Criterios de priorización:
- Consultas sobre tratamiento de datos sensibles (salud, menores, biometría) → **PRIORITARIO**
- Consultas sobre transferencias internacionales → **PRIORITARIO**
- Consultas sobre derechos ARCO → Normal
- Consultas informativas generales → Normal

### 2. Asignarse la tarea

Al abrir una tarea:
1. Hacer clic en **"Asignarme"** — cambia estado a `InProgress`
2. Leer la pregunta original del usuario
3. Leer la respuesta generada por el asistente
4. Revisar las citaciones de fuentes (normas legales referenciadas)

### 3. Criterios de evaluación

**Aprobar si:**
- La respuesta cita correctamente artículos de la Ley 21.719
- Los plazos indicados son correctos (ej: derechos ARCO = 15 días hábiles)
- No hay instrucciones que puedan llevar a incumplimiento
- La respuesta reconoce su limitación cuando corresponde

**Rechazar si:**
- Cita artículos incorrectos o inexistentes
- Proporciona plazos incorrectos
- Omite requisitos obligatorios (ej: no menciona el Registro de Tratamiento)
- Podría inducir a eludir controles de protección de datos

### 4. Registrar la decisión

Al aprobar:
- Puede agregar notas opcionales si hay matices importantes
- Clic en **"Aprobar"**

Al rechazar:
- **Las notas son obligatorias** — explicar por qué la respuesta es incorrecta
- Incluir la corrección sugerida si es posible
- Clic en **"Rechazar"**

---

## Alertas automáticas

El sistema envía una alerta si hay tareas sin revisar después de **2 horas**.
El responsable de cumplimiento recibirá una notificación en el canal configurado.

Si el volumen es alto (>20 tareas pendientes), notificar al Compliance Officer para
coordinar asignación de revisores adicionales.

---

## Consultas de análisis

### Ver tareas asignadas a mí (SQL directo en emergencias)

```sql
SELECT rt.id, rt.interaction_id, rt.created_at, rt.status,
       mi.question, LEFT(mi.answer, 500) as answer_preview
FROM mcp.mcp_review_tasks rt
JOIN mcp.mcp_interactions mi ON rt.interaction_id = mi.id
WHERE rt.assigned_to = '<mi-user-id>'
  AND rt.status = 'InProgress'
ORDER BY rt.created_at ASC;
```

### Métricas semanales de HITL

```sql
SELECT
    DATE_TRUNC('week', created_at) AS week,
    COUNT(*) FILTER (WHERE status = 'Open') AS open,
    COUNT(*) FILTER (WHERE status = 'Approved') AS approved,
    COUNT(*) FILTER (WHERE status = 'Rejected') AS rejected,
    AVG(EXTRACT(EPOCH FROM (completed_at - created_at))/3600)
        FILTER (WHERE completed_at IS NOT NULL) AS avg_resolution_hours
FROM mcp.mcp_review_tasks
WHERE created_at > NOW() - INTERVAL '8 weeks'
GROUP BY 1
ORDER BY 1 DESC;
```

---

## Escalación

| Situación | Acción |
|---|---|
| Respuesta con implicaciones legales graves | Escalar a abogado de cumplimiento antes de rechazar |
| Volumen >50 tareas pendientes | Notificar Compliance Officer para redistribuir carga |
| Duda sobre criterio de evaluación | Consultar `docs/evidata-backend-docs/14-especificacion-rat-mvp.md` |
