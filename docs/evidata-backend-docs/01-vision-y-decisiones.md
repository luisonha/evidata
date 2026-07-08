# Visión y decisiones de producto/backend

## 0. Identidad del producto

El producto es **Evidata**. Su descriptor principal de marca es **Digital Trust Operating System**. Los ejes de valor aprobados desde el documento de marca son:

- **Trazabilidad medible**.
- **Gobernanza operativa**.
- **Automatización con criterio**.

**ATLAS** se usa sólo como nombre de proyecto técnico, investigación o alcance interno. Cuando este documento hable de **ATLAS Opción B**, se refiere al alcance técnico de implementación backend para Evidata, no al nombre comercial del producto.

No se deben incorporar términos de naming o categoría no aprobados en la documentación técnica.

## 1. Objetivo de ATLAS Opción B

ATLAS Opción B es una plataforma SaaS multi-tenant para apoyar el cumplimiento operativo de la Ley 21.719 mediante una base determinística de cumplimiento y una capa progresiva de asistencia legal/operativa.

El alcance inicial se concentra en:

| Componente | Propósito |
|---|---|
| Inventario/RAT | Registrar actividades de tratamiento, finalidades, bases legales, categorías de datos, sistemas, responsables, retención, transferencias declaradas y evidencias asociadas. |
| Evidencias | Probar decisiones, revisiones, documentos, respuestas, aprobaciones, respaldos y cierres de brecha. |
| Catálogo legal | Mantener fuentes, obligaciones, derechos, bases de licitud, taxonomías y versiones. |
| Brechas | Convertir hallazgos del inventario en acciones de remediación. |
| Reportes | Exportar RAT, brechas y paquetes de evidencia. |
| Búsqueda | Consultar fuentes legales y datos internos autorizados. |
| MCP contextual | Asistente legal-operativo con citas, abstención, control de riesgo y revisión humana, implementado al final de Opción B. |

ATLAS no debe venderse como cumplimiento automático ni como abogado IA. Debe operar como sistema de trazabilidad, evidencia, workflow y asistencia.

## 2. Decisiones cerradas

| Área | Decisión |
|---|---|
| Lenguaje/backend | .NET 10 |
| Arquitectura | Core modular evolutivo + Azure Functions para capacidades desacopladas |
| Frontend | Se cerrará después; backend se define primero |
| Base transaccional | PostgreSQL, pendiente confirmación definitiva |
| Documentos | Azure Blob Storage |
| Identidad | Entra External ID o equivalente administrado; pendiente confirmación definitiva |
| Secretos | Azure Key Vault |
| Mensajería inicial | Azure Storage Queues |
| Mensajería futura | Service Bus sólo bajo gatillos definidos |
| Patrón de eventos | Outbox neutral al broker |
| IA/MCP | No como primer producto; primero core determinístico |
| Microservicios | Sólo para procesos asíncronos claros desde el MVP |

## 3. Decisión sobre microservicios

No se implementará una arquitectura de microservicios puros desde el día 1 porque eleva el costo, la complejidad de DevOps, el QA distribuido, la observabilidad, la seguridad interservicios y el riesgo de sobreconstrucción.

La decisión actual es una arquitectura evolutiva:

| Tipo | Qué incluye |
|---|---|
| Core modular | Tenants, usuarios, seguridad contextual, RAT, evidencias metadata, brechas, workflow, auditoría, búsqueda interactiva, MCP interactivo inicial. |
| Azure Functions desacopladas | Procesamiento documental, indexación, reportes, notificaciones, MCP batch, security jobs y maintenance jobs. |
| Futuro microservicio | MCP completo, Evidence, RAT, Consentimiento, DSAR o Incidentes sólo cuando exista volumen, equipo, SLA o necesidad enterprise. |

## 4. Decisión sobre el MCP

El MCP no debe ser el primer módulo comercial completo. Debe llegar al final de Opción B, cuando ya existan:

- catálogo legal versionado;
- RAT con tratamientos reales;
- evidencias;
- brechas;
- workflow;
- búsqueda legal e interna;
- auditoría de interacciones.

Puede existir antes una capacidad interna o batch para apoyar construcción, verificación de citas o generación de borradores, pero el MCP comercial debe apoyarse en datos confiables y auditables.

## 5. Decisión sobre seguridad como microservicio

No se separará todo “seguridad” como microservicio único desde el MVP. Seguridad es transversal y parte de ella depende del contexto del dominio.

Se separa o externaliza desde el inicio:

- autenticación con Entra External ID;
- secretos con Key Vault;
- security jobs asíncronos;
- auditoría y alertas por eventos.

Se mantiene dentro del core:

- autorización contextual;
- permisos sobre RAT, evidencias, brechas y MCP;
- tenant isolation;
- políticas de acciones sensibles.

## 6. Decisión sobre mensajería

No se usará Azure Service Bus como requisito inicial porque todavía no existe evidencia de necesidad de pub/sub avanzado, sesiones, filtros por suscripción o integración enterprise.

Para reducir costo y complejidad inicial:

- se usará Azure Storage Queues;
- se implementará Outbox desde el MVP;
- se definirá una abstracción neutral de mensajería;
- los destinos serán lógicos, no acoplados a un proveedor;
- Service Bus podrá incorporarse por destino cuando existan gatillos.

## 7. Principios no negociables

1. Todo dato funcional debe tener tenant.
2. Todo acceso debe validar tenant, usuario y permiso.
3. Todo cambio relevante debe auditarse.
4. Todo evento debe estar versionado.
5. Toda integración asíncrona debe ser idempotente.
6. La lógica de negocio no debe depender directamente del SDK de colas.
7. El core debe funcionar sin MCP.
8. El MCP no debe certificar cumplimiento.
9. Toda respuesta legal del MCP debe citar fuente o abstenerse.
10. Toda extracción futura debe estar soportada por límites de dominio claros.

---

## Trazabilidad de fuentes

Este documento debe leerse con trazabilidad explícita a las dos fuentes entregadas:

- `ley-datos-personales.pdf`: aporta el fundamento normativo-operativo de la Ley 21.719, los elementos computables, los patrones de workflow, evidencia, DSAR, consentimiento, incidentes, EIPD, RAT funcional, transferencias y decisiones automatizadas.
- `ATLAS-Informe-Factibilidad.pdf`: aporta la recomendación de producto, la priorización de Opción B, los riesgos comerciales/técnicos, la arquitectura recomendada, el enfoque de implementación, los costos y la decisión de no construir una suite completa desde el inicio.

Las decisiones técnicas de este documento no deben interpretarse como exigencias legales directas. Cuando una decisión sea una recomendación de arquitectura, se declara como decisión del proyecto y no como mandato normativo.