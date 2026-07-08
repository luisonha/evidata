# 14 — Especificación RAT MVP

## 1. Propósito

Este documento define el módulo Processing Inventory / RAT para el MVP de ATLAS Opción B. Es el núcleo funcional del producto y debe permitir levantar, revisar, versionar, evidenciar y exportar actividades de tratamiento.

El RAT en ATLAS no se presenta como obligación nominal universal. Se presenta como inventario funcional para organizar la información necesaria de cumplimiento, transparencia, trazabilidad y evidencia.

## 2. Objetivo del módulo

Permitir que un tenant responda operativamente:

1. Qué tratamientos de datos personales realiza.
2. Para qué finalidad.
3. Bajo qué base de licitud.
4. Qué categorías de datos utiliza.
5. Qué titulares están afectados.
6. Qué sistemas y proveedores intervienen.
7. Si existen datos sensibles, NNA, biometría, transferencias o decisiones automatizadas.
8. Qué medidas y evidencias respaldan el tratamiento.
9. Qué brechas deben corregirse.
10. Qué versión fue revisada y aprobada.

## 3. Alcance del MVP

Incluye:

- creación de tratamientos;
- edición por secciones;
- validaciones de completitud;
- flags de riesgo;
- revisión y aprobación;
- versionado;
- asociación de evidencias;
- generación de brechas;
- exportación RAT;
- trazabilidad y auditoría.

No incluye:

- portal externo de titulares;
- DSAR completo;
- consentimiento multicanal;
- incidentes;
- EIPD avanzada;
- gestión contractual completa de proveedores;
- motor jurídico automático de cumplimiento;
- certificación de cumplimiento.

## 4. Secciones del tratamiento

| Sección | Objetivo | Obligatoria para aprobación |
|---|---|---:|
| Identificación | Nombre, código, descripción y área. | Sí |
| Responsables | Dueño funcional, revisor legal, contacto operativo. | Sí |
| Finalidad | Finalidad explícita del tratamiento. | Sí |
| Base de licitud | Base legal y justificación. | Sí |
| Categorías de datos | Datos tratados y sensibilidad. | Sí |
| Titulares | Categorías de personas afectadas. | Sí |
| Sistemas | Sistemas internos involucrados. | Sí si aplica |
| Proveedores | Encargados/proveedores asociados. | Sí si aplica |
| Transferencias | Países, destinatarios y base declarada. | Sí si aplica |
| Retención | Período y justificación. | Recomendado; configurable como obligatorio |
| Medidas de seguridad | Medidas técnicas y organizativas. | Sí para aprobación si hay datos sensibles o alto riesgo |
| Evidencias | Documentos o pruebas asociadas. | Requeridas según regla |
| Brechas | Hallazgos pendientes. | No bloquea aprobación salvo brecha crítica |
| Revisión | Comentarios, aprobación y versión. | Sí |

## 5. Campos mínimos

### 5.1 Identificación

| Campo | Tipo funcional | Obligatorio | Regla |
|---|---|---:|---|
| Código | Texto único por tenant | Sí | Puede generarse automáticamente. |
| Nombre | Texto | Sí | Debe ser claro y no duplicado funcionalmente. |
| Descripción | Texto | No | Recomendado. |
| Área | Referencia | Sí para aprobación | Debe existir en catálogo de unidades. |
| Estado | Catálogo | Sí | Controlado por workflow. |

### 5.2 Responsables

| Campo | Obligatorio | Regla |
|---|---:|---|
| Dueño funcional | Sí | Debe ser usuario activo del tenant. |
| Revisor legal | Recomendado | Obligatorio si tratamiento tiene flags de riesgo alto. |
| Revisor seguridad | Condicional | Obligatorio si hay datos sensibles, biometría, NNA o decisiones automatizadas. |

### 5.3 Finalidad y base de licitud

| Campo | Obligatorio | Regla |
|---|---:|---|
| Finalidad | Sí | Debe ser específica y explícita. |
| Base de licitud | Sí | Debe seleccionarse de catálogo versionado. |
| Justificación base | Sí | Debe explicar por qué aplica. |
| Evidencia base | Condicional | Requerida si se basa en consentimiento u otra base que el tenant deba acreditar. |

### 5.4 Categorías de datos

| Campo | Obligatorio | Regla |
|---|---:|---|
| Categoría de dato | Sí | Al menos una. |
| Sensibilidad | Sí | Determina flags. |
| Comentario | No | Útil para datos no estándar. |

### 5.5 Titulares

Debe registrar categorías como clientes, empleados, postulantes, usuarios web, proveedores, niños/niñas/adolescentes u otras definidas por tenant.

## 6. Estados del tratamiento

| Estado | Descripción | Quién puede moverlo |
|---|---|---|
| Draft | En construcción. | ProcessOwner, ComplianceAdmin. |
| InReview | En revisión. | Submit por ProcessOwner/ComplianceAdmin. |
| ChangesRequested | Devuelto con observaciones. | LegalReviewer/ComplianceAdmin. |
| Approved | Versión aprobada. | LegalReviewer/ComplianceAdmin/TenantOwner. |
| Active | Tratamiento vigente. | Automático o decisión tenant. |
| Deprecated | Reemplazado por otro tratamiento. | ComplianceAdmin. |
| Archived | Cerrado/retirado. | ComplianceAdmin/TenantOwner. |

## 7. Transiciones permitidas

| Desde | Hacia | Condición |
|---|---|---|
| Draft | InReview | Completitud mínima de revisión. |
| InReview | ChangesRequested | Observaciones obligatorias. |
| ChangesRequested | InReview | Cambios aplicados y comentario opcional. |
| InReview | Approved | Completitud de aprobación y permisos. |
| Approved | Active | Automático o manual según configuración. |
| Active | Draft nueva versión | Cambio material post aprobación. |
| Active | Deprecated | Existe reemplazo o decisión formal. |
| Active | Archived | Motivo obligatorio. |
| Deprecated | Archived | Motivo obligatorio. |

## 8. Reglas de completitud

### 8.1 Completitud mínima para enviar a revisión

Debe existir:

- nombre;
- área;
- dueño funcional;
- finalidad;
- base de licitud preliminar;
- al menos una categoría de datos;
- al menos una categoría de titulares.

### 8.2 Completitud para aprobación

Debe existir:

- todo lo anterior;
- justificación de base de licitud;
- sistemas asociados o declaración de no aplicabilidad;
- proveedores asociados o declaración de no aplicabilidad;
- retención o decisión documentada de pendiente;
- medidas de seguridad mínimas si hay flags de riesgo;
- evidencia mínima según base de licitud y clasificación;
- revisión legal completada;
- revisión de seguridad si aplica;
- no existencia de brechas críticas abiertas sin aceptación formal.

## 9. Flags de riesgo

| Flag | Gatillo |
|---|---|
| SensitiveData | Se asocia categoría sensible. |
| ChildrenData | Se asocia categoría NNA. |
| BiometricData | Se asocia categoría biométrica. |
| InternationalTransfer | Se declara país/destinatario fuera de Chile. |
| AutomatedDecision | Se declara decisión automatizada o perfilamiento. |
| MissingLegalBasisEvidence | Falta evidencia para base que requiere prueba. |
| MissingRetention | No existe retención definida. |
| MissingSecurityMeasures | Faltan medidas declaradas. |
| CriticalGapOpen | Hay brecha crítica abierta. |

## 10. Brechas automáticas iniciales

El MVP puede generar brechas sugeridas, no decisiones jurídicas finales.

| Condición | Brecha sugerida |
|---|---|
| Sin evidencia de base de licitud | “Falta evidencia de licitud del tratamiento.” |
| Datos sensibles sin causal/refuerzo | “Tratamiento sensible requiere revisión y respaldo específico.” |
| NNA sin revisión | “Tratamiento de NNA requiere revisión reforzada.” |
| Transferencia internacional sin base declarada | “Transferencia internacional sin mecanismo documentado.” |
| Decisión automatizada sin explicación/revisión | “Automatización requiere revisión de derechos y transparencia.” |
| Sin medidas de seguridad | “No existen medidas de seguridad documentadas.” |
| Sin retención | “No se ha definido período de conservación.” |

## 11. Versionado

1. Cada aprobación crea un snapshot inmutable.
2. El snapshot debe incluir tratamiento, relaciones, flags, evidencias vinculadas y aprobaciones.
3. Cambios materiales sobre Active generan nueva versión en Draft.
4. Debe poder compararse versión actual vs versión anterior.
5. Las evidencias vinculadas a una versión aprobada no deben desaparecer; si se eliminan lógicamente, la versión debe conservar referencia histórica.

## 12. Evidencias mínimas por sección

| Sección | Evidencia sugerida |
|---|---|
| Base de licitud | Política, contrato, consentimiento, fundamento interno, matriz legal. |
| Finalidad | Política de privacidad, documentación de proceso, ficha de servicio. |
| Proveedores | Contrato, anexo, orden de servicio, evaluación. |
| Transferencias | Contrato, cláusula, evaluación, justificación. |
| Seguridad | Política, control, procedimiento, evidencia técnica. |
| Retención | Política, procedimiento, regla interna. |

## 13. Exportación RAT

Formatos MVP:

- Excel estructurado;
- PDF ejecutivo;
- JSON interno para integración futura, no necesariamente expuesto al usuario final.

Contenido mínimo:

- tratamiento;
- finalidad;
- base;
- categorías;
- titulares;
- sistemas;
- proveedores;
- transferencias;
- retención;
- medidas;
- evidencias asociadas;
- brechas;
- estado;
- versión;
- aprobaciones.

## 14. Seguridad RAT

1. Edición por estado.
2. Aprobación restringida.
3. Exportación auditada.
4. Flags sensibles visibles sólo a roles autorizados si el tenant así lo configura.
5. Toda aprobación requiere motivo.
6. Todo cambio material debe registrarse.
7. Consultores externos sólo ven tratamientos asignados o permitidos por scope.
8. MCP sólo puede usar tratamientos que el usuario pueda ver.

## 15. APIs relacionadas

Los contratos base están en `12-contratos-api-backend.md`. Este módulo requiere APIs para:

- crear tratamiento;
- listar tratamientos;
- ver detalle;
- actualizar secciones;
- asociar evidencia;
- enviar a revisión;
- solicitar cambios;
- aprobar;
- archivar;
- exportar;
- ver versiones;
- comparar versiones.

## 16. Criterios de aceptación MVP

1. Un tenant puede crear un tratamiento en Draft.
2. El sistema valida completitud antes de revisión.
3. El sistema genera flags de riesgo básicos.
4. El sistema permite asociar evidencias.
5. El sistema permite enviar a revisión.
6. El sistema permite aprobar con permisos adecuados.
7. La aprobación crea versión inmutable.
8. El sistema genera brechas sugeridas.
9. El RAT puede exportarse.
10. Todas las acciones críticas quedan auditadas.

## 17. Pendientes

1. Confirmar catálogo exacto de bases de licitud MVP.
2. Confirmar si retención bloquea aprobación o sólo genera brecha.
3. Confirmar nivel de detalle de sistemas/proveedores en Opción B.
4. Confirmar si la exportación RAT debe seguir plantilla específica de WikiGuías o formato propio.
5. Confirmar si NNA y biometría requieren aprobaciones dobles desde MVP.

---

## Trazabilidad de fuentes

Este documento debe leerse con trazabilidad explícita a las dos fuentes entregadas:

- `ley-datos-personales.pdf`: aporta el fundamento normativo-operativo de la Ley 21.719, los elementos computables, los patrones de workflow, evidencia, DSAR, consentimiento, incidentes, EIPD, RAT funcional, transferencias y decisiones automatizadas.
- `ATLAS-Informe-Factibilidad.pdf`: aporta la recomendación de producto, la priorización de Opción B, los riesgos comerciales/técnicos, la arquitectura recomendada, el enfoque de implementación, los costos y la decisión de no construir una suite completa desde el inicio.

Las decisiones técnicas de este documento no deben interpretarse como exigencias legales directas. Cuando una decisión sea una recomendación de arquitectura, se declara como decisión del proyecto y no como mandato normativo.
