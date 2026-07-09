# 09 — Testing strategy

## 1. Principio

En esta iteración, las pruebas que validan integración, seguridad y comportamiento regulatorio no pueden cerrar con dobles de persistencia, autorización o reglas reales, porque `06-quality-gates-and-dod.md` exige explícitamente **no mockear persistencia principal en integración**, **no mockear autorización real en autorización/integración** y **no mockear reglas de brecha bajo prueba**; además, los gates de `main` piden evidencia específica para API, RBAC, auditoría, ciclo de vida de evidencia, reglas de brecha y exportación, por lo que un test que sólo pase contra mocks no demuestra que el sistema real cumple el DoD.

## 2. Matriz de estrategia por tipo de test

| Tipo de test | Mecanismo obligatorio | Prohibido | Paquete/herramienta |
|---|---|---|---|
| Unit / domain | Probar lógica pura sobre objetos de dominio y dobles puros de dependencias externas, sin IO real | Levantar API completa, usar Postgres/Azurite para lógica puramente de dominio, validar sólo wiring | xUnit, NSubstitute, fakes/manual stubs |
| Integration API | Host real de la API con `WebApplicationFactory<Program>` y persistencia Postgres real | `UseInMemoryDatabase`, mocks del repositorio principal, validar sólo `200 OK` | `Microsoft.AspNetCore.Mvc.Testing`, `Testcontainers.PostgreSql` |
| Security / RBAC | Host real + autorización real + persistencia real para permisos, tenants y claims | Mockear `AuthorizationEvaluator`, roles, claims o storage de permisos | `WebApplicationFactory<Program>`, `Testcontainers.PostgreSql` |
| Audit | Ejecutar la acción crítica real y verificar el registro auditado persistido | Mockear escritor de auditoría o afirmar sólo que “se llamó” un método | `WebApplicationFactory<Program>`, `Testcontainers.PostgreSql` |
| Evidence lifecycle | Persistencia real + blobs reales emulados para upload, asociación, validación y auditoría | Mockear blob storage o metadatos de evidencia | `Testcontainers.PostgreSql`, `Testcontainers.Azurite`, `WebApplicationFactory<Program>` |
| Gap rules | Ejecutar la regla real sobre fixtures determinísticos y persistir resultado real | Mockear motor/regla bajo prueba o usar EF in-memory para flujos que pretenden ser integración | `Testcontainers.PostgreSql`, `WebApplicationFactory<Program>` |
| Export | Invocar flujo real de exportación y verificar artefacto/metadata/auditoría en infraestructura de prueba | Mockear generación final o validar sólo DTOs aislados | `WebApplicationFactory<Program>`, `Testcontainers.PostgreSql`, `Testcontainers.Azurite` si el export usa blobs |

## 3. Decisión concreta de infraestructura

La base estándar para integración y seguridad será:

- `Testcontainers.PostgreSql` **>= 4.4.0** para persistencia real.
- `Testcontainers.Azurite` **4.4.0** para blobs, ya presente hoy en `tests/Evidata.Tests.Integration/Evidata.Tests.Integration.csproj`.
- `WebApplicationFactory<Program>` para levantar el host real de `src/Evidata.Api` en pruebas de integración y seguridad.

Se elige esta combinación y no otra por tres razones: (1) reduce complejidad al usar sólo los componentes reales que el backend ya depende en producción (`UseNpgsql` en la API); (2) Azurite ya está instalado, así que no introduce una segunda estrategia para blobs; y (3) con .NET 10 y minimal hosting, `WebApplicationFactory` es la opción natural para probar el pipeline HTTP real sin inventar un host paralelo.

## 4. Regla de migración

Todo test que hoy use `UseInMemoryDatabase` y en realidad cubra flujos de **integración**, **security/RBAC** o **gap rules** debe migrarse a Postgres real vía `Testcontainers.PostgreSql`. La búsqueda actual muestra `UseInMemoryDatabase` sólo en `tests/Evidata.Tests.Unit`, incluyendo casos de seguridad (`CrossTenantIsolationTests`, `AuthorizationEvaluatorTests`) y gaps (`RatGapDetectionTests`), lo que confirma que el estado actual todavía mezcla escenarios de integración lógica con EF in-memory. En cambio, los tests puramente unitarios de dominio, sin IO ni contrato de infraestructura, pueden mantenerse con fakes puros o almacenamiento en memoria local al test.

## 5. Fixtures deterministicos

Los fixtures deben nombrarse en **kebab-case orientado a escenario de negocio**, siguiendo el patrón ya visible en `04-rbac-audit-evidence-gaps-contract.md`, por ejemplo `pa-transfer-no-country`. La carpeta sugerida es `tests/Fixtures/`, con subcarpetas por contexto (`gap-rules/`, `rbac/`, `evidence/`, `exports/`) para que los distintos proyectos de test reutilicen el mismo catálogo. Regla obligatoria: **un fixture = un escenario de negocio reproducible**, con datos explícitos, IDs/fechas controladas y sin aleatoriedad, de forma que el nombre del fixture explique la condición que debe disparar el resultado esperado.
