# 21 — Guía de implementación por módulo

## 1. Propósito

Este documento corrige el gap detectado en v0.2: la implementación de un módulo parecía exigir leer todo el paquete documental. La versión 0.3 introduce un modelo de **paquetes de implementación por módulo**.

El objetivo es que cada equipo pueda implementar un módulo leyendo un conjunto acotado de documentos, sin perder consistencia con la arquitectura global, la seguridad, los datos, la mensajería y la infraestructura Azure.

## 2. Principio de autonomía documental

Cada paquete de módulo debe contener, como mínimo:

1. objetivo del módulo;
2. alcance incluido;
3. alcance excluido;
4. responsabilidades;
5. límites de dominio;
6. roles involucrados;
7. arquitectura interna;
8. entidades y ownership de datos;
9. APIs necesarias;
10. eventos/mensajes emitidos o consumidos;
11. seguridad específica;
12. infraestructura Azure necesaria;
13. pruebas mínimas;
14. Definition of Done;
15. dependencias con otros módulos;
16. documentos adicionales obligatorios.

La regla es: **un desarrollador no debe buscar reglas críticas en diez documentos distintos para implementar un módulo**. Las reglas críticas se repiten dentro del paquete del módulo, y los documentos globales quedan como fuente de referencia.

## 3. Qué se mantiene global

Aunque los paquetes sean autocontenidos, algunas decisiones siguen siendo globales:

| Decisión global | Documento fuente |
|---|---|
| Estilo core modular + Functions | 02-arquitectura-general-backend.md |
| Seguridad transversal | 04-seguridad-transversal.md |
| Storage Queues + Outbox | 05-mensajeria-asincronia-y-outbox.md |
| Modelo de datos completo | 11-modelo-datos-backend.md |
| Contratos API globales | 12-contratos-api-backend.md |
| RBAC global | 13-matriz-rbac-politicas-autorizacion.md |
| Eventos y mensajes | 16-contratos-eventos-mensajes.md |
| Infraestructura Azure | 17-blueprint-infraestructura-azure.md |
| Testing | 18-estrategia-testing-calidad.md |
| Definition of Done | 19-definition-of-done.md |

Los paquetes de módulo no reemplazan estos documentos. Los resumen y aterrizan para implementación.

## 4. Lectura mínima para implementar un módulo

Para implementar cualquier módulo:

1. Leer `21-guia-implementacion-por-modulo.md`.
2. Leer `22-matriz-dependencias-documentales.md`.
3. Leer el paquete específico del módulo (`30` a `39`).
4. Leer `19-definition-of-done.md`.
5. Leer documentos globales adicionales sólo si el paquete lo exige.

## 5. Criterio para modificar documentos

Si una decisión afecta a varios módulos:

1. Se actualiza el documento global correspondiente.
2. Se actualizan todos los paquetes de módulo afectados.
3. Se actualiza `00-navegacion.md` si cambia estructura o estado.
4. Se actualiza `99-evaluacion-autocritica.md` si cambia la suficiencia documental.

Ejemplo: si se decide cambiar Storage Queues por Service Bus, deben cambiar al menos:

- `05-mensajeria-asincronia-y-outbox.md`;
- `16-contratos-eventos-mensajes.md`;
- `17-blueprint-infraestructura-azure.md`;
- paquetes 32, 36, 37, 38, 39;
- `00-navegacion.md`;
- `99-evaluacion-autocritica.md`.

## 6. Cómo evitar duplicación peligrosa

La documentación v0.3 acepta duplicar reglas críticas dentro de los paquetes de módulo para mejorar implementación. Para evitar contradicciones:

- cada paquete debe tener una sección **Decisiones globales aplicadas**;
- cada regla copiada debe ser breve y operacional;
- la fuente de verdad extendida debe indicarse en **Documentos relacionados**;
- si una regla global cambia, se debe buscar su mención en todos los paquetes.

## 7. Estructura estándar de un paquete de módulo

Cada paquete 30-39 sigue esta estructura:

1. Propósito.
2. Decisiones globales aplicadas.
3. Alcance incluido.
4. Alcance excluido.
5. Responsabilidades.
6. Límites de dominio.
7. Roles y permisos.
8. Arquitectura del módulo.
9. Datos y ownership.
10. APIs.
11. Eventos, mensajes y asincronía.
12. Seguridad del módulo.
13. Infraestructura Azure.
14. Observabilidad.
15. Testing mínimo.
16. Definition of Done.
17. Dependencias.
18. Riesgos y controles.
19. Documentos que sí debe leer el equipo.
20. Documentos que no son obligatorios para este módulo.

## 8. Política de implementación incremental

Ningún módulo debe esperar a que toda la plataforma esté completa. La implementación debe avanzar por cortes verticales seguros.

Orden recomendado:

1. Foundation, seguridad y auditoría.
2. Legal Knowledge mínimo.
3. Documentos y procesamiento.
4. Evidence.
5. RAT.
6. Workflow y Gaps.
7. Reporting.
8. Search.
9. MCP.
10. Notificaciones y jobs operativos ampliados.

## 9. Política de calidad mínima por módulo

Un módulo no se considera implementable si no tiene:

- ownership claro de datos;
- tenant isolation;
- permisos definidos;
- al menos un flujo feliz y flujos de error;
- auditoría de acciones sensibles;
- pruebas unitarias;
- pruebas de integración;
- pruebas cross-tenant si maneja datos de tenant;
- eventos o mensajes definidos si participa en asincronía;
- DoD propio.

## 10. Cierre del gap detectado

El gap original era: **para implementar cualquier módulo parecía necesario leer todo el documento completo**.

La corrección aplicada es:

- creación de guía de implementación modular;
- creación de matriz de dependencias documentales;
- creación de paquetes autocontenidos por módulo;
- actualización del documento de navegación;
- actualización de la evaluación autocrítica.

Resultado esperado: un equipo puede implementar un módulo leyendo de forma obligatoria entre 3 y 5 documentos, no los 20+ documentos del paquete completo.

---

## Trazabilidad de fuentes

Este documento debe leerse con trazabilidad explícita a las dos fuentes entregadas:

- `ley-datos-personales.pdf`: aporta el fundamento normativo-operativo de la Ley 21.719, los elementos computables, los patrones de workflow, evidencia, DSAR, consentimiento, incidentes, EIPD, RAT funcional, transferencias y decisiones automatizadas.
- `ATLAS-Informe-Factibilidad.pdf`: aporta la recomendación de producto, la priorización de Opción B, los riesgos comerciales/técnicos, la arquitectura recomendada, el enfoque de implementación, los costos y la decisión de no construir una suite completa desde el inicio.

Las decisiones técnicas de este documento no deben interpretarse como exigencias legales directas. Cuando una decisión sea una recomendación de arquitectura, se declara como decisión del proyecto y no como mandato normativo.
