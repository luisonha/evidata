# 22 — Matriz de dependencias documentales por módulo

## 1. Propósito

Este documento define qué debe leer cada equipo para implementar un módulo sin tener que revisar todo el paquete documental.

Clasificación:

- **Obligatorio**: debe leerse antes de implementar.
- **Consulta**: sólo se lee si aparece una duda o si se modifica la decisión global.
- **No requerido**: no es necesario para implementar ese módulo.

## 2. Documentos globales base

| Código | Documento |
|---|---|
| NAV | 00-navegacion.md |
| VIS | 01-vision-y-decisiones.md |
| ARC | 02-arquitectura-general-backend.md |
| AZR | 03-infraestructura-azure.md / 17-blueprint-infraestructura-azure.md |
| SEC | 04-seguridad-transversal.md / 13-matriz-rbac-politicas-autorizacion.md |
| MSG | 05-mensajeria-asincronia-y-outbox.md / 16-contratos-eventos-mensajes.md |
| DATA | 11-modelo-datos-backend.md |
| API | 12-contratos-api-backend.md |
| TEST | 18-estrategia-testing-calidad.md |
| DOD | 19-definition-of-done.md |
| BKL | 20-backlog-backend-fases.md |

## 3. Matriz por paquete de implementación

| Paquete | Obligatorio | Consulta | No requerido inicialmente |
|---|---|---|---|
| 30 Foundation, Seguridad y Auditoría | 21, 22, 30, SEC, DATA, API, DOD | ARC, AZR, TEST, BKL | 14, 15, 38 |
| 31 Legal Knowledge | 21, 22, 31, DATA, API, SEC, DOD | ARC, AZR, TEST | 32, 33, 38 |
| 32 Documentos y Procesamiento | 21, 22, 32, DATA, API, SEC, MSG, AZR, DOD | TEST, ARC | 14, 38 |
| 33 Evidence | 21, 22, 33, 15, DATA, API, SEC, DOD | MSG, AZR, TEST | 38 |
| 34 RAT / Processing Inventory | 21, 22, 34, 14, DATA, API, SEC, DOD | 15, MSG, TEST | 38 |
| 35 Workflow y Gaps | 21, 22, 35, DATA, API, SEC, DOD | 14, 15, MSG, TEST | 38 |
| 36 Reporting | 21, 22, 36, DATA, API, SEC, MSG, AZR, DOD | 14, 15, TEST | 38 |
| 37 Search e Indexing | 21, 22, 37, DATA, API, SEC, MSG, AZR, DOD | 31, 32, 33, TEST | 14 |
| 38 MCP Contextual | 21, 22, 38, SEC, DATA, API, MSG, DOD | 14, 15, 31, 37, AZR, TEST | Ninguno crítico |
| 39 Notificaciones y Jobs | 21, 22, 39, MSG, SEC, AZR, DOD | DATA, API, TEST | 14, 15, 38 |

## 4. Lectura mínima por fase

| Fase | Documentos obligatorios |
|---|---|
| Fase 0 — Base técnica | 00, 01, 02, 03, 04, 05, 17, 19, 21, 22 |
| Fase 1 — Foundation | 21, 22, 30, 13, 17, 19 |
| Fase 2 — Legal/Documentos | 21, 22, 31, 32, 16, 17, 19 |
| Fase 3 — Evidence | 21, 22, 33, 15, 19 |
| Fase 4 — RAT | 21, 22, 34, 14, 19 |
| Fase 5 — Workflow/Gaps | 21, 22, 35, 19 |
| Fase 6 — Reporting/Search | 21, 22, 36, 37, 16, 17, 19 |
| Fase 7 — MCP | 21, 22, 38, 04, 13, 16, 19 |

## 5. Reglas de independencia de equipos

Un equipo puede implementar un módulo si cumple estas condiciones:

1. Ha leído el paquete correspondiente.
2. Ha revisado los documentos obligatorios indicados en esta matriz.
3. Ha validado dependencias con otros módulos.
4. Ha definido contratos API/eventos antes de codificar.
5. Ha convertido el DoD del paquete en tareas de aceptación.

## 6. Riesgo de lectura insuficiente

| Riesgo | Control |
|---|---|
| Equipo implementa permisos distintos a RBAC global | Cada paquete copia permisos mínimos y referencia SEC. |
| Equipo implementa eventos sin idempotencia | Cada paquete copia reglas mínimas de MSG. |
| Equipo usa infraestructura no alineada | Cada paquete lista recursos Azure específicos. |
| Equipo crea entidades duplicadas | Cada paquete declara ownership de datos. |
| Equipo no prueba cross-tenant | Cada paquete incluye pruebas mínimas de seguridad. |

## 7. Criterio de cierre del gap documental

El gap se considera cerrado cuando:

- cada módulo tenga paquete autocontenido;
- cada paquete indique documentos obligatorios y no obligatorios;
- cada paquete contenga seguridad, datos, APIs, eventos, Azure, testing y DoD;
- `00-navegacion.md` indique el nuevo modelo de lectura;
- `99-evaluacion-autocritica.md` evalúe si la corrección fue suficiente.

---

## Trazabilidad de fuentes

Este documento debe leerse con trazabilidad explícita a las dos fuentes entregadas:

- `ley-datos-personales.pdf`: aporta el fundamento normativo-operativo de la Ley 21.719, los elementos computables, los patrones de workflow, evidencia, DSAR, consentimiento, incidentes, EIPD, RAT funcional, transferencias y decisiones automatizadas.
- `ATLAS-Informe-Factibilidad.pdf`: aporta la recomendación de producto, la priorización de Opción B, los riesgos comerciales/técnicos, la arquitectura recomendada, el enfoque de implementación, los costos y la decisión de no construir una suite completa desde el inicio.

Las decisiones técnicas de este documento no deben interpretarse como exigencias legales directas. Cuando una decisión sea una recomendación de arquitectura, se declara como decisión del proyecto y no como mandato normativo.
