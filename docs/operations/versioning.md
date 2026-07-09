# Política de Versionamiento — Evidata

**Versión del documento:** 1.0  
**Fecha:** 2026-07-09  
**Estado:** Vigente

---

## 1. Principio rector

El versionamiento de Evidata sigue **SemVer 2.0** (https://semver.org/lang/es/).  
**El incremento de versión es siempre deliberado y manual.** No existe incremento automático por commit ni por build — esto es intencional: cada cambio de versión requiere una decisión consciente de qué impacto tiene el cambio en los consumidores de la API.

---

## 2. Formato de versión

```
MAJOR.MINOR.PATCH.BUILD[+<git-hash>]
```

| Segmento | Ejemplo | Descripción |
|----------|---------|-------------|
| `MAJOR` | `2` | Cambio que rompe compatibilidad (breaking change) |
| `MINOR` | `1` | Nueva funcionalidad, sin romper lo existente |
| `PATCH` | `3` | Corrección de bug, sin cambio funcional |
| `BUILD` | `247` | Conteo total de commits git — se incrementa automáticamente |
| `+<git-hash>` | `+a1b2c3d` | Metadato de build — identifica el commit exacto compilado |

**Ejemplo completo:** `1.3.2.247+a1b2c3d`

### Propiedades de ensamblado .NET

| Propiedad | Valor | Uso |
|-----------|-------|-----|
| `AssemblyVersion` | `1.0.0.0` | Estable — solo cambia en MAJOR. Controla compatibilidad binaria. |
| `FileVersion` | `1.0.0.247` | 4 bloques — visible en propiedades del archivo en Windows/macOS. |
| `InformationalVersion` | `1.0.0.247+a1b2c3d` | Completo — aparece en logs, `/api/version`, Application Insights. |

### Por qué el BUILD se incrementa automáticamente

El BUILD es el resultado de `git rev-list --count HEAD` — el número total de commits en el repositorio. Cada commit agrega 1. Es:
- **Determinista**: dos builds del mismo commit producen el mismo BUILD
- **Monotónico**: siempre crece, nunca retrocede
- **Sin dependencias**: no requiere servidor de build ni NuGet packages adicionales

El `+<git-hash>` complementa al BUILD: si dos branches tienen el mismo conteo de commits pero diferente historia, el hash los diferencia.

---

## 3. Cuándo incrementar cada segmento

### PATCH — correcciones de errores

Incrementar cuando:
- Se corrige un bug sin cambiar el contrato de la API
- Se mejora el rendimiento sin cambios de interfaz
- Se actualiza documentación o logging
- Se corrigen advertencias del compilador

```
1.0.0 → 1.0.1
```

### MINOR — nueva funcionalidad

Incrementar cuando:
- Se agrega un nuevo endpoint
- Se agrega un campo opcional a un request/response existente
- Se agrega un nuevo módulo
- Se agrega funcionalidad que no afecta a los consumidores existentes

```
1.0.1 → 1.1.0  (PATCH se reinicia a 0)
```

### MAJOR — cambio que rompe compatibilidad

Incrementar cuando:
- Se elimina o renombra un endpoint
- Se cambia el tipo o formato de un campo existente
- Se elimina un campo del response
- Se cambia la semántica de un parámetro
- Se modifica la autenticación o el esquema de autorización

```
1.1.0 → 2.0.0  (MINOR y PATCH se reinician a 0)
```

---

## 4. Dónde vive la versión

La versión base se define en un único lugar:

```
/Directory.Build.props
```

```xml
<VersionPrefix>1.0.0</VersionPrefix>
```

Este archivo aplica a **todos los proyectos de la solución**. No se define versión individual por proyecto.

El git hash se inyecta en el build desde:
- `scripts/local/start.sh` — desarrollo local
- `.github/workflows/ci.yml` — CI/CD

---

## 5. Dónde verificar la versión en ejecución

### Log de arranque

Al iniciar la API, se registra en el log:

```
[INF] Evidata.Api Evidata API iniciada · versión 1.0.0+a1b2c3d · entorno Production
```

Visible en:
- **Aspire Dashboard** → evidata-api → Logs
- **Azure Application Insights** → Traces (en producción)

### Endpoint HTTP

```http
GET /api/version
```

Respuesta:

```json
{
  "version": "1.0.0+a1b2c3d",
  "fileVersion": "1.0.0.0",
  "assemblyName": "Evidata.Api",
  "buildTime": "2026-07-09T07:00:00Z",
  "environment": "Production"
}
```

Este endpoint está excluido del OpenAPI público pero es accesible en todos los entornos.

---

## 6. Proceso de release

### Prerequisitos
- Todos los tests pasan (`dotnet test`)
- Smoke test en staging: 0 FAIL
- PR aprobado y mergeado a `develop`

### Pasos

```bash
# 1. Determinar el tipo de cambio (MAJOR/MINOR/PATCH)
#    Ver sección 3 de este documento.

# 2. Actualizar la versión en Directory.Build.props
#    Editar: <VersionPrefix>X.Y.Z</VersionPrefix>

# 3. Crear el branch de release
git checkout develop
git pull origin develop
git checkout -b dev/YYYY/MM/DD/release-vX.Y.Z

# 4. Commitear el bump de versión
git add Directory.Build.props
git commit -m "chore: bump version to X.Y.Z"

# 5. PR → develop, aprobación, merge

# 6. Desde develop, crear el tag de release
git checkout develop
git pull origin develop
git tag -a vX.Y.Z -m "Release vX.Y.Z"
git push origin vX.Y.Z

# 7. El CI de producción se dispara con el tag (ver ci-release.yml)
```

### Tags de versión

Los tags siguen el formato `vMAJOR.MINOR.PATCH`:

```
v1.0.0   ← primer release
v1.0.1   ← patch
v1.1.0   ← minor
v2.0.0   ← major (breaking)
```

---

## 7. Pre-releases y entornos

| Entorno | Versión | Ejemplo |
|---------|---------|---------|
| Local dev | `X.Y.Z+<hash>` | `1.0.0+df62858` |
| CI (PR) | `X.Y.Z+<hash>` | `1.0.0+abc1234` |
| Staging | `X.Y.Z+<hash>` | `1.0.0+release-hash` |
| Producción | `X.Y.Z+<hash>` | `1.0.0+tagged-hash` |

No se usan pre-release identifiers (`-alpha`, `-beta`, `-rc`) en este momento. Si se necesitan en el futuro, usar formato `X.Y.Z-rc.1`.

---

## 8. Convención de commits (referencia)

Aunque el incremento de versión es manual, los commits deben comunicar claramente el tipo de cambio para facilitar la decisión:

| Prefijo | Tipo | Impacto en versión |
|---------|------|--------------------|
| `fix:` | Bug fix | PATCH |
| `feat:` | Nueva funcionalidad | MINOR |
| `feat!:` o `BREAKING CHANGE:` | Cambio breaking | MAJOR |
| `chore:` | Mantenimiento, build, deps | ninguno |
| `docs:` | Documentación | ninguno |
| `refactor:` | Refactorización sin cambio funcional | ninguno |
| `test:` | Tests | ninguno |
| `perf:` | Optimización de rendimiento | PATCH o ninguno |

---

## 9. Lo que NO hace este sistema

- **No incrementa automáticamente** la versión por cada commit o build. Esto es deliberado.
- **No usa NuGet packages** de versionamiento (MinVer, Nerdbank.GitVersioning). La política actual cubre las necesidades sin dependencias adicionales.
- **No versiona endpoints individualmente** (`/api/v1/`, `/api/v2/`). Si se requiere versionamiento de API en el futuro, evaluar `Asp.Versioning.Mvc`.

---

## 10. Checklist de release

- [ ] `Directory.Build.props` actualizado con nueva versión
- [ ] Tipo de cambio justificado (MAJOR/MINOR/PATCH) según sección 3
- [ ] Tests unitarios: 0 fallos
- [ ] Smoke test en staging: 0 FAIL
- [ ] `docs/openapi/openapi.json` actualizado (`bash scripts/local/export-openapi.sh`)
- [ ] Tag `vX.Y.Z` creado y pusheado
- [ ] Log de arranque confirma versión correcta en producción
