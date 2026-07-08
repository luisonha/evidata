# 19 — Definition of Done

## 1. Propósito

Este documento define cuándo una fase, módulo, endpoint, Function o documento se considera terminado. Su objetivo es evitar entregas ambiguas y reducir retrabajo.

## 2. Definition of Done general backend

Un componente backend se considera terminado cuando:

1. Cumple la especificación funcional del módulo.
2. Tiene contratos API o mensaje documentados.
3. Implementa validación de entrada.
4. Implementa autorización backend.
5. Respeta tenant isolation.
6. Registra auditoría cuando corresponde.
7. Emite eventos/outbox si corresponde.
8. Tiene pruebas unitarias relevantes.
9. Tiene pruebas de integración si toca persistencia o infraestructura.
10. Tiene observabilidad mínima.
11. Maneja errores funcionales estándar.
12. Está documentado en Markdown correspondiente.
13. Está desplegado en ambiente de validación.
14. No deja decisiones críticas implícitas.

## 3. DoD para módulos core

| Criterio | Obligatorio |
|---|---:|
| Entidades definidas | Sí |
| Migraciones aplicadas | Sí |
| APIs documentadas | Sí |
| Permisos definidos | Sí |
| Políticas contextuales implementadas | Sí si aplica |
| Auditoría | Sí si hay acciones sensibles |
| Eventos Outbox | Sí si hay asincronía |
| Pruebas unitarias | Sí |
| Pruebas integración | Sí |
| Pruebas cross-tenant | Sí |
| Documentación actualizada | Sí |

## 4. DoD para Azure Functions

Una Function se considera terminada cuando:

1. Tiene trigger definido.
2. Tiene contrato de mensaje versionado.
3. Valida mensaje y tenant.
4. Es idempotente.
5. Maneja reintentos.
6. Maneja poison messages.
7. No registra datos sensibles en logs.
8. Usa Managed Identity o secreto desde Key Vault.
9. Actualiza estado del job o entidad correspondiente.
10. Tiene prueba de consumo correcto y fallo controlado.
11. Tiene métricas y logs correlacionados.

## 5. DoD para APIs

Cada endpoint debe tener:

- método y ruta;
- permiso requerido;
- request definido;
- response definido;
- errores funcionales;
- validaciones;
- auditoría;
- eventos emitidos;
- pruebas de autorización;
- prueba de tenant isolation;
- documentación actualizada.

## 6. DoD para RAT MVP

El módulo RAT está terminado cuando:

1. Permite crear tratamiento Draft.
2. Permite editar secciones definidas.
3. Valida completitud para revisión.
4. Genera flags de riesgo básicos.
5. Permite asociar evidencias.
6. Permite enviar a revisión.
7. Permite solicitar cambios.
8. Permite aprobar con rol autorizado.
9. Crea snapshot versionado e inmutable.
10. Genera brechas sugeridas.
11. Exporta RAT.
12. Audita acciones críticas.
13. Impide edición directa de versión aprobada.
14. Pasa pruebas cross-tenant.

## 7. DoD para Evidence MVP

Evidence está terminado cuando:

1. Permite crear evidencia.
2. Permite clasificar sensibilidad.
3. Permite asociar a entidades autorizadas.
4. Impide asociación cross-tenant.
5. Permite descarga controlada.
6. Audita descarga sensible.
7. Permite eliminación lógica.
8. Conserva historial de evidencias en versiones aprobadas.
9. Genera evidence pack asíncrono.
10. Pasa pruebas de permisos y exportación.

## 8. DoD para mensajería

La mensajería MVP está terminada cuando:

1. Outbox funciona end-to-end.
2. Storage Queues recibe mensajes desde publicador abstracto.
3. Al menos una Function consume mensaje real.
4. Existe idempotencia.
5. Existe manejo de poison.
6. Los mensajes tienen versión.
7. Los destinos son lógicos.
8. Se puede monitorear backlog y fallos.

## 9. DoD para seguridad

Seguridad base está terminada cuando:

1. Autenticación validada contra proveedor definido.
2. Usuario se resuelve contra tenant.
3. Roles y permisos funcionan.
4. Acceso cross-tenant se deniega y audita.
5. Acciones sensibles requieren motivo.
6. Descargas sensibles se auditan.
7. MCP con contexto requiere permiso específico.
8. Key Vault se usa para secretos.
9. Managed Identity configurada donde aplique.
10. Pruebas de seguridad mínimas pasan.

## 10. DoD para infraestructura

Infraestructura MVP está terminada cuando:

1. Recursos están creados por IaC o proceso reproducible.
2. Ambientes están separados.
3. API despliega automáticamente.
4. PostgreSQL funciona con backup configurado.
5. Storage tiene contenedores y colas.
6. Key Vault está integrado.
7. App Insights recibe telemetría.
8. Alertas críticas están configuradas.
9. Health checks funcionan.
10. Costos estimados están documentados.

## 11. DoD documental

Un documento está terminado cuando:

1. Tiene propósito claro.
2. Define alcance y límites.
3. Es consistente con navegación.
4. No contradice decisiones vigentes.
5. Indica pendientes explícitos.
6. Puede ser usado por desarrollo o arquitectura.
7. Está actualizado en el índice.

## 12. Condición para aceptar responsabilidad de desarrollo

No se debe aceptar responsabilidad de entrega funcional cerrada si:

- el módulo no tiene DoD;
- no hay contratos API/mensajes;
- no hay permisos definidos;
- no hay modelo de datos;
- no hay criterios de prueba;
- hay decisiones bloqueantes pendientes sin resolución.

---

## Trazabilidad de fuentes

Este documento debe leerse con trazabilidad explícita a las dos fuentes entregadas:

- `ley-datos-personales.pdf`: aporta el fundamento normativo-operativo de la Ley 21.719, los elementos computables, los patrones de workflow, evidencia, DSAR, consentimiento, incidentes, EIPD, RAT funcional, transferencias y decisiones automatizadas.
- `ATLAS-Informe-Factibilidad.pdf`: aporta la recomendación de producto, la priorización de Opción B, los riesgos comerciales/técnicos, la arquitectura recomendada, el enfoque de implementación, los costos y la decisión de no construir una suite completa desde el inicio.

Las decisiones técnicas de este documento no deben interpretarse como exigencias legales directas. Cuando una decisión sea una recomendación de arquitectura, se declara como decisión del proyecto y no como mandato normativo.
