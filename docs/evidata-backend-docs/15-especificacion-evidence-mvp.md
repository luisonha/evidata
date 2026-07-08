# 15 — Especificación Evidence MVP

## 1. Propósito

Este documento define el módulo Evidence para ATLAS Opción B. La evidencia es la capa que convierte el inventario y los workflows en cumplimiento demostrable. Sin evidencia, el sistema sería sólo un repositorio declarativo.

## 2. Objetivo del módulo

Permitir registrar, clasificar, asociar, consultar, auditar y exportar evidencias relacionadas con tratamientos, bases de licitud, revisiones, brechas, documentos, reportes y futuras solicitudes regulatorias.

## 3. Alcance del MVP

Incluye:

- creación de evidencia;
- clasificación;
- asociación a entidades;
- relación con documentos/versiones;
- control de acceso;
- auditoría de descarga;
- eliminación lógica;
- generación de evidence pack;
- trazabilidad de vínculo y desvinculación.

No incluye:

- firma electrónica avanzada;
- custodia legal certificada;
- DLP avanzado;
- OCR obligatorio;
- análisis automático de suficiencia jurídica;
- integración con repositorios externos;
- cadena de custodia legal formal, salvo metadata básica.

## 4. Tipos de evidencia MVP

| Tipo | Uso |
|---|---|
| Policy | Política, aviso, procedimiento o documento normativo interno. |
| Contract | Contrato, anexo, orden, acuerdo con proveedor. |
| ConsentProof | Prueba de consentimiento o revocación. |
| LegalBasisSupport | Respaldo de base de licitud. |
| SecurityMeasure | Evidencia de medida técnica/organizativa. |
| ReviewRecord | Evidencia de revisión legal o seguridad. |
| ApprovalRecord | Evidencia de aprobación. |
| GapResolution | Evidencia de cierre de brecha. |
| SystemScreenshot | Captura o exportación de sistema. |
| EmailProof | Correo o comunicación respaldatoria. |
| MeetingRecord | Acta o minuta. |
| ReportArtifact | Reporte generado por ATLAS. |
| Other | Tipo no clasificado. |

## 5. Estados

| Estado | Descripción |
|---|---|
| Draft | Evidencia creada pero no validada. |
| Active | Evidencia utilizable. |
| Superseded | Reemplazada por otra. |
| Archived | Conservada por historial. |
| Deleted | Eliminación lógica. |

## 6. Clasificación de sensibilidad

| Clasificación | Control |
|---|---|
| Public | Puede aparecer en reportes generales. |
| Internal | Visible a usuarios internos con permisos. |
| Confidential | Requiere permiso explícito para descarga/exportación. |
| Sensitive | Requiere auditoría reforzada, motivo y restricciones MCP. |

## 7. Asociación de evidencia

Una evidencia puede asociarse a múltiples entidades, pero cada asociación debe tener motivo.

Entidades soportadas en MVP:

- ProcessingActivity;
- ProcessingActivityVersion;
- ComplianceGap;
- Review;
- Approval;
- ReportJob;
- LegalObligation;
- Document;
- McpInteraction si se usa como soporte de revisión, no como fuente legal definitiva.

## 8. Reglas de negocio

1. No se puede asociar evidencia a entidad de otro tenant.
2. No se puede descargar evidencia sin permiso.
3. No se puede eliminar físicamente evidencia usada en versión aprobada.
4. La eliminación debe ser lógica y auditada.
5. La evidencia Sensitive requiere motivo para descarga.
6. Todo link/unlink genera auditoría.
7. Evidence pack debe registrar alcance, filtros y usuario.
8. Los documentos vinculados deben estar procesados o marcarse como no procesables.
9. Una evidencia puede reemplazar a otra mediante relación explícita Supersedes.
10. Una evidencia no determina por sí sola cumplimiento; sólo soporta decisiones.

## 9. Evidence Pack

### 9.1 Definición

Paquete exportable de evidencias y metadata asociada a un alcance definido.

### 9.2 Alcances MVP

| Alcance | Contenido |
|---|---|
| Por tratamiento | Tratamiento, versión, evidencias, revisiones, brechas asociadas. |
| Por brecha | Brecha, resolución, evidencias y comentarios. |
| Por tenant | Selección controlada de tratamientos/evidencias. |
| Por reporte RAT | Evidencias citadas en exportación RAT. |

### 9.3 Contenido mínimo

- metadata del paquete;
- solicitante;
- fecha;
- filtros usados;
- entidades incluidas;
- listado de evidencias;
- versiones documentales;
- hashes;
- auditoría mínima de generación;
- advertencia de alcance.

## 10. Seguridad

1. Descargas auditadas.
2. Enlaces temporales.
3. Blob privado.
4. Metadata tenant obligatoria.
5. Clasificación obligatoria.
6. Control de evidencia sensible.
7. Motivo obligatorio para exportación masiva.
8. Consultor externo requiere scope explícito.
9. MCP no puede usar evidencia Sensitive sin permiso y minimización.
10. Evidence pack generado debe expirar según política.

## 11. Arquitectura

### 11.1 Core

El core administra metadata, asociaciones, permisos y solicitudes de exportación.

### 11.2 Document Processing Function

Procesa archivo asociado, extrae texto si aplica y actualiza estado documental.

### 11.3 Report Generation Function

Genera evidence pack de forma asíncrona.

### 11.4 Storage

Archivos en Blob Storage. Metadata en PostgreSQL.

## 12. APIs relacionadas

Definidas en `12-contratos-api-backend.md`:

- crear evidencia;
- asociar evidencia;
- consultar evidencia;
- solicitar descarga;
- solicitar evidence pack;
- consultar job;
- eliminar lógicamente;
- reemplazar evidencia.

## 13. Auditoría

Deben auditarse:

- creación;
- cambio de clasificación;
- asociación;
- desvinculación;
- descarga;
- exportación;
- eliminación lógica;
- reemplazo;
- acceso denegado sensible.

## 14. Criterios de aceptación MVP

1. Se puede crear evidencia con o sin documento asociado.
2. Se puede clasificar evidencia.
3. Se puede asociar a tratamiento y brecha.
4. Se impide asociación cross-tenant.
5. Se registra auditoría de descarga.
6. Se genera evidence pack asíncrono.
7. Se conserva referencia histórica en versiones aprobadas.
8. Se aplican permisos por clasificación.
9. Se puede reemplazar evidencia sin perder historial.
10. Se puede consultar evidencia desde RAT.

## 15. Pendientes

1. Confirmar tipos de evidencia exactos para el primer piloto.
2. Confirmar tiempo de expiración de reportes/evidence packs.
3. Confirmar si se requiere antivirus desde MVP.
4. Confirmar si OCR es parte del MVP o fase posterior.
5. Confirmar retención mínima de evidencia eliminada lógicamente.

---

## Trazabilidad de fuentes

Este documento debe leerse con trazabilidad explícita a las dos fuentes entregadas:

- `ley-datos-personales.pdf`: aporta el fundamento normativo-operativo de la Ley 21.719, los elementos computables, los patrones de workflow, evidencia, DSAR, consentimiento, incidentes, EIPD, RAT funcional, transferencias y decisiones automatizadas.
- `ATLAS-Informe-Factibilidad.pdf`: aporta la recomendación de producto, la priorización de Opción B, los riesgos comerciales/técnicos, la arquitectura recomendada, el enfoque de implementación, los costos y la decisión de no construir una suite completa desde el inicio.

Las decisiones técnicas de este documento no deben interpretarse como exigencias legales directas. Cuando una decisión sea una recomendación de arquitectura, se declara como decisión del proyecto y no como mandato normativo.
