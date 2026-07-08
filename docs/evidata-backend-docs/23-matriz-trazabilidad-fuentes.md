# 23 — Matriz de trazabilidad de fuentes

## 1. Objetivo

Este documento corrige debilidades detectadas en versiones previas: la documentación generada estaba demasiado apoyada en el informe de factibilidad de ATLAS, no dejaba suficientemente explícito el uso de `ley-datos-personales.pdf` y tampoco incorporaba de forma correcta el documento `construccion-de-marca.pdf`.

Desde esta versión, toda la documentación debe reconocer dos fuentes base:

| Fuente | Uso principal dentro de la especificación |
|---|---|
| `ley-datos-personales.pdf` | Marco legal, oportunidades de negocio, elementos computables de la Ley 21.719, DSAR, consentimiento, incidentes, transferencias, EIPD, RAT funcional y patrones de modelado regulatorio. |
| `ATLAS-Informe-Factibilidad.pdf` | Decisión de producto, recomendación Opción B, arquitectura técnica, riesgos, costos, implementación, onboarding, validación comercial, IA/MCP y priorización de módulos. |
| `construccion-de-marca.pdf` | Identidad de producto, marca Evidata, descriptor Digital Trust Operating System, ejes de valor, benchmark, posicionamiento comercial y canvas de solución. |

## 2. Regla de uso documental

Ningún módulo debe derivar su justificación sólo desde el informe de factibilidad. Cada paquete implementable debe distinguir:

1. **Fundamento normativo-operativo:** proviene principalmente de `ley-datos-personales.pdf`.
2. **Fundamento de producto y factibilidad:** proviene principalmente de `ATLAS-Informe-Factibilidad.pdf`.
3. **Decisiones arquitectónicas propias del paquete:** provienen de la conversación de arquitectura y deben quedar documentadas como decisiones del proyecto, no como afirmaciones legales.

## 3. Trazabilidad por área

| Área de especificación | Fuente normativa-operativa | Fuente de producto/factibilidad | Observación |
|---|---|---|---|
| Visión general del producto | `ley-datos-personales.pdf` | `ATLAS-Informe-Factibilidad.pdf` | La necesidad surge de la Ley 21.719; la forma de producto se prioriza en ATLAS. |
| Opción B | Apoyo indirecto por obligaciones computables | Fuente principal | Opción B es decisión de factibilidad, no exigencia legal. |
| RAT / Inventario | Fuente principal para contenido funcional y elementos computables | Fuente principal para priorización como wedge | El RAT no se debe presentar como obligación nominal universal, sino como soporte operativo fuerte. |
| Evidence | Fuente principal para accountability, prueba y trazabilidad | Fuente principal para arquitectura y módulo | Evidence materializa la capacidad de demostrar acciones y decisiones. |
| DSAR futuro | Fuente principal | Fuente secundaria | No entra en Opción B inicial, pero debe quedar preparado. |
| Consentimiento futuro | Fuente principal | Fuente secundaria | No se implementa completo en Opción B, pero impacta Evidence y Legal Knowledge. |
| Incidentes futuro | Fuente principal | Fuente secundaria | Se prepara desde eventos, evidencia y seguridad. |
| EIPD futuro | Fuente principal | Fuente secundaria | Debe quedar como extensión de RAT y Workflow. |
| Encargados y transferencias futuro | Fuente principal | Fuente secundaria | Se modela inicialmente como campos/flags de RAT, no como módulo completo. |
| MCP contextual | Fuente indirecta, como acceso guiado a fuentes y obligaciones | Fuente principal | No debe venderse como abogado IA ni como cumplimiento automático. |
| Seguridad transversal | Fuente normativa indirecta | Fuente técnica/arquitectónica | Se complementa con decisiones de arquitectura Azure y multi-tenant. |
| Mensajería / Outbox / Queues | No aplica directamente | Decisión técnica derivada | Es una decisión de implementación, no una exigencia de la ley. |

## 4. Impacto en paquetes implementables

Cada paquete de módulo debe incluir una sección llamada **Trazabilidad de fuentes**, con esta estructura mínima:

- `ley-datos-personales.pdf`: indica qué obligación, patrón operativo o elemento computable soporta el módulo.
- `ATLAS-Informe-Factibilidad.pdf`: indica qué decisión de producto, factibilidad, riesgo o arquitectura soporta el módulo.
- `Decisión del proyecto`: indica la decisión arquitectónica adoptada en esta especificación.

## 5. Corrección aplicada en v0.4

En esta versión se agrega trazabilidad explícita de ambas fuentes en:

- navegación documental;
- visión y decisiones;
- arquitectura general;
- módulos core;
- microservicios y Functions;
- modelo de datos;
- contratos API;
- especificación RAT;
- especificación Evidence;
- contratos de eventos;
- paquetes autocontenidos por módulo;
- evaluación autocrítica.

## 6. Criterio de aceptación documental

Un documento o paquete se considera suficientemente trazable si permite responder:

1. ¿Qué parte del módulo viene de una necesidad normativa u operativa de Ley 21.719?
2. ¿Qué parte viene de una decisión de producto de ATLAS?
3. ¿Qué parte es una decisión arquitectónica del proyecto?
4. ¿Qué partes son futuras y no pertenecen al alcance de Opción B inicial?

## 7. Riesgo residual

Aunque esta matriz mejora la trazabilidad, todavía se requiere revisión legal profesional antes de vender el producto con claims regulatorios. La documentación técnica no debe transformarse en una opinión legal ni en garantía de cumplimiento.


## 8. Uso del documento `construccion-de-marca.pdf`

Este documento se usa para fijar la identidad del producto y evitar decisiones de naming no respaldadas por la marca.

Aporta:

- nombre base del producto: **Evidata**;
- descriptor principal: **Digital Trust Operating System**;
- ejes de valor: trazabilidad medible, gobernanza operativa y automatización con criterio;
- consolidación de Evidata como nombre de producto;
- posicionamiento comercial y benchmark;
- canvas de la solución.

ATLAS queda como proyecto técnico o investigación de factibilidad, no como marca comercial.
