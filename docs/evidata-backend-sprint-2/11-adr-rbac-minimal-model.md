# 11 — ADR: Modelo mínimo de RBAC (Role-Based Access Control)

## ADR: Modelo mínimo de RBAC para satisfacer SEC-* requirements

### Estado

**Propuesta para esta iteración (P1-RBAC)**

---

## 1. Contexto

El backend de Evidata **carece hoy de infraestructura de roles/RBAC en la capa de autorización**. Los hechos observados en la investigación:

- **Identidad actual**: `UserProfile.cs` modela sólo identidad externa (email, nombre, proveedor, `TenantId`), sin rol.
- **Contexto actual**: `ICurrentUserContext` expone `UserId`, `TenantId`, `Email`, `IsAuthenticated` — **sin roles**.
- **JWT actual**: Los tokens se configuran en `Program.cs` con claims estándar (`userId`, `tenantId`, `email`), sin claim de rol.
- **Módulo Security**: Existe `src/Modules/Security/` con `Role`, `Permission`, `UserRoleAssignment`, `IAuthorizationEvaluator` ya implementados, pero:
  - No hay seeding de los 7 roles mínimos requeridos.
  - Los claims del JWT no se agregan con roles.
  - `ICurrentUserContext` no expone roles al resto de módulos.
  - El `IAuthorizationEvaluator` existe pero no está integrado en los endpoints de negocio.

- **Especificación RBAC**: `docs/evidata-backend-sprint-2/04-rbac-audit-evidence-gaps-contract.md` sección 1 define:
  - **7 roles mínimos**: `TenantOwner`, `ComplianceAdmin`, `ProcessOwner`, `LegalReviewer`, `SecurityReviewer`, `Auditor`, `Viewer`.
  - **6 permisos críticos** (SEC-APP-001 a SEC-EVDOWN-001) que deben ser evaluados por rol y contexto.
  - **Mapeo de reviewDomain → rol**: `Legal` → `LegalReviewer`, `Security` → `SecurityReviewer`.

- **Multi-tenancy**: El sistema ya aísla datos por `TenantId` (ADR-004 en `10-registro-de-decisiones-arquitectonicas.md`). Un usuario puede tener roles distintos en tenants distintos — la tabla `UserRoleAssignment` ya modela esto con `(UserId, RoleId, TenantId)`.

- **Seguridad local**: El modo `LocalDev` y `TestAuth` (`26-seguridad-desarrollo-local-sin-entra.md`) permiten development sin Azure Entra, inyectando claims vía headers; el mismo mecanismo se usará para roles en desarrollo.

### Brecha de implementación actual

Aunque el módulo Security existe con dominio y repositorios, **falta el puente de integración**:

1. No hay seeding de los 7 roles en migraciones.
2. No hay extensión de `ICurrentUserContext` para exponer `IReadOnlyList<string> RoleNames`.
3. No hay lógica que agregue claims de rol al JWT al autenticar.
4. No hay extensión de `LocalDevCurrentUserContext` para inyectar roles desde headers de dev.
5. No hay `PermissionCalculator` declarativo que interprete las 6 reglas críticas.

---

## 2. Decisión

Se adopta el siguiente **modelo mínimo de RBAC** para cumplir el contrato del doc 04 sección 1:

### 2.1 Decisión sobre modelo de datos: Entidad Role (no enum)

**Elección**: Mantener la entidad `Role` (ya implementada en `src/Modules/Security/Domain/Role.cs`) en lugar de un enum.

**Justificación**:

- **Extensibilidad**: Futuros roles custom por tenant (P2+) sin recompilación.
- **Auditoría**: Cada rol tiene `Id`, `Name`, `Description`, `IsSystemRole` — trazabilidad clara.
- **Permisos associados**: La relación `Role ↔ RolePermission ↔ Permission` es clara y escalable.
- **Datos vs. código**: Separar la configuración de seguridad del código de aplicación es una mejor práctica.

**Rechazo de enum**: Un enum C# estaría hardcoded en el binario, imposibilitando:
- Agregar permisos sin recompilación.
- Auditar cambios en definiciones de rol.
- Implementar políticas custom por tenant o ambiente sin redeploy.

### 2.2 Modelo de asignación de roles: Tabla UserRoleAssignment

**Estructura** (ya existe en `src/Modules/Security/Domain/UserRoleAssignment.cs`):

```
UserRoleAssignment
├─ Id: Guid
├─ UserId: Guid (FK a UserProfile)
├─ RoleId: Guid (FK a Role)
├─ TenantId: Guid (FK a Tenant — ITenantScoped)
├─ AssignedAt: DateTime
└─ (opcional P2: AssignedBy: Guid, ExpiresAt: DateTime?)
```

**Reglas**:

- Un usuario puede tener **múltiples roles en el mismo tenant** (e.g., `ProcessOwner` + `SecurityReviewer`).
- Un usuario puede tener **roles distintos en tenants distintos**.
- Una asignación es la tupla `(UserId, RoleId, TenantId)` — no se permiten duplicados (PK compuesta).
- Borrar una asignación es la única forma de revoke; no hay soft-delete.

### 2.3 Extensión de ICurrentUserContext para exponer roles

**Nueva interfaz**:

```csharp
public interface ICurrentUserContext
{
    Guid UserId { get; }
    Guid TenantId { get; }
    string Email { get; }
    bool IsAuthenticated { get; }
    
    // Nuevo
    IReadOnlyList<string> RoleNames { get; }  // ["TenantOwner", "ComplianceAdmin", ...]
}
```

**Justificación del tipo**:

- `IReadOnlyList<string>` (nombres de roles, no IDs) — los módulos consumen nombres semánticos, no IDs.
- No se expone `IReadOnlyList<RoleDto>` completo para mantener separación: módulos de negocio no deben depender del shape completo de `Role`.
- Los nombres se normalizan como `"TenantOwner"` (constantes en archivo central, ej. `RoleConstants.cs`).

**Implementación en las 3 estrategias de autenticación**:

1. **`JwtCurrentUserContext`** (JWT real de producción):
   - Lee los claims `"role"` del JWT (array o lista CSV).
   - Mantiene caché local de nombres.

2. **`LocalDevCurrentUserContext`** (desarrollo local):
   - Lee header `X-Evidata-Dev-Roles` como CSV: `"TenantOwner,ProcessOwner"`.
   - Permite testing sin BD de roles.

3. **`NullCurrentUserContext`** (fallback sin autenticación):
   - Retorna `RoleNames = []` (lista vacía).

### 2.4 Emisión de claims de rol en JWT

**Punto de integración**: Al emitir el token (hoy no existe endpoint explícito; la auth es vía Entra/LocalDev).

**Estrategia**:

- **En producción (Entra)**: El token viene de Entra con claims estándar. Un middleware o filter post-login puede:
  1. Leer `userId` del token.
  2. Consultar `UserRoleAssignment` para ese usuario + `TenantId`.
  3. Agregar claim `"role"` con los nombres de roles (formato: `"role1,role2"` o array, según convención JWT).
  4. Re-emitir o enriquecer el token con `AddClaim()`.

- **En local (desarrollo)**:
  - Los headers `X-Evidata-Dev-Roles` se usan directamente en `LocalDevCurrentUserContext`.
  - No hay token JWT real generado en desarrollo puro; la auth se hace via `LocalDevAuthenticationHandler` que construye un `ClaimsIdentity` con claims incluidos `"role"`.

- **Cambio mínimo al JWT de T-P0-07**:
  - El claim existente `"userId"` y `"tenantId"` se mantienen intactos.
  - Se agrega un nuevo claim `"role"` (formato a definir: `"role1,role2"` o array JSON).
  - El validador JWT no valida contenido de `"role"` — es información de contexto, no de seguridad de token.

**Ejemplo de payload JWT enriquecido**:

```json
{
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "tenantId": "550e8400-e29b-41d4-a716-446655440001",
  "email": "user@tenant.com",
  "role": "ProcessOwner,LegalReviewer",
  "iat": 1234567890,
  "exp": 1234571490,
  "iss": "https://evidata-auth.local",
  "aud": "evidata-api"
}
```

### 2.5 Estrategia declarativa de permisos: PermissionCalculator

**No se implementa en este ADR**, pero se describe el enfoque:

El `PermissionCalculator` (a construir en P1-RBAC o P1-GapManagement) evaluará las 6 reglas críticas del doc 04:

```
SEC-APP-001: ApproveProcessingActivity 
  → ProcessOwner no puede aprobar su propia actividad.
  
SEC-ACT-001: ActivateProcessingActivity 
  → Sólo TenantOwner/ComplianceAdmin.
  
SEC-EV-001: ValidateEvidence 
  → Rol según reviewDomain (Legal → LegalReviewer, Security → SecurityReviewer).
  
SEC-GAP-001: AcceptGapWithRisk 
  → Sólo TenantOwner/ComplianceAdmin.
  
SEC-EXP-001: GenerateOfficialExport 
  → Sólo roles autorizados, actividad aprobada/activa.
  
SEC-EVDOWN-001: DownloadEvidence 
  → Viewer no descarga evidencia sensible.
```

**Enfoque propuesto (declarativo)**:

Crear una tabla/mapa centralizado `PermissionRule`:

```csharp
public class PermissionRule
{
    public string PermissionCode { get; set; }  // "ApproveProcessingActivity"
    public string[] AllowedRoles { get; set; }  // ["TenantOwner", "ComplianceAdmin", ...]
    public Func<IAuthorizationContext, bool>? CustomValidator { get; set; }  // e.g., ownershipCheck
}
```

Interfaz de contexto:

```csharp
public interface IAuthorizationContext
{
    ICurrentUserContext CurrentUser { get; }
    Guid ResourceId { get; }
    string ResourceType { get; }
    Dictionary<string, object> Metadata { get; }  // ownership, state, sensitivity, etc.
}
```

Evaluador:

```csharp
public class PermissionCalculator
{
    public async Task<bool> EvaluateAsync(
        string permissionCode, 
        IAuthorizationContext context, 
        CancellationToken ct = default)
    {
        var rule = _rules.FirstOrDefault(r => r.PermissionCode == permissionCode);
        if (rule is null) return false;  // unknown permission → deny
        
        // 1. Role check
        if (!rule.AllowedRoles.Contains(context.CurrentUser.RoleNames))
            return false;
        
        // 2. Custom check (e.g., ownership)
        if (rule.CustomValidator is not null)
            return await rule.CustomValidator(context);
        
        return true;
    }
}
```

**No es código final**, es un esbozo. La implementación real puede usar:
- Atributos `[RequireRole("TenantOwner")]` en controladores.
- Policies de ASP.NET Authorization.
- Policy handlers delegados al dominio.

---

## 3. Riesgos y alternativas consideradas

### Alternativa 1: Claims-only RBAC (sin persistencia de roles)

**Descripción**: Los roles vienen sólo en el JWT/claims, sin tabla `UserRoleAssignment`.

**Rechazo**:
- No hay auditoría local de cambios de rol.
- La gestión de roles queda fuera del backend — depende 100% de Entra o proveedor externo.
- No se puede testear RBAC en local sin hardcodear roles en código.
- No se puede revocar rol sin esperar renovación del token (TTL).

**Mantener**: La persistencia de `UserRoleAssignment` permite control local.

---

### Alternativa 2: Usar ASP.NET Core Identity Roles directamente

**Descripción**: Reemplazar el módulo `Security` con `Microsoft.AspNetCore.Identity` que incluye `IdentityRole`, `UserRole`, etc.

**Rechazo**:
- El proyecto **no usa ASP.NET Core Identity** (tampoco para usuarios — usa `UserProfile` custom).
- Agregar Identity ahora rompe la arquitectura modular sin beneficio real.
- Los 7 roles son simples — no necesitamos features de Identity como password policies, 2FA, etc.
- El módulo `Security` ya existe y es suficiente.

**Mantener**: Usar la entidad `Role` existente.

---

### Alternativa 3: Enum de roles hardcoded

**Descripción**: Usar un enum C# `enum RoleName { TenantOwner, ComplianceAdmin, ... }`.

**Rechazo**:
- Roles hardcoded en binario — imposible agregar roles sin redeploy.
- Sin auditoría de cambios en definición de rol.
- Testing local requiere recompilación.

**Mantener**: Usar entidad `Role` con persistencia.

---

### Riesgo operacional: Circular dependency entre modules

**Riesgo**: El módulo `ProcessingInventory` (o `GapManagement`) debe depender de `IAuthorizationEvaluator` (Security). Si Security a su vez necesita constructos de ProcessingInventory para evaluar reglas, hay circular dependency.

**Mitigation**:
- `IAuthorizationEvaluator` es interface en Security, agnóstico a dominio de negocio.
- Las reglas específicas (e.g., "ProcessOwner no puede aprobar su propia actividad") viven en el command handler / domain service del módulo de negocio, no en Security.
- Security proporciona sólo: `HasPermissionAsync(userId, tenantId, resource, action)` — la evaluación de ownership/context es responsabilidad del módulo que consume.

---

### Riesgo de performance: Queries de roles

**Riesgo**: Cada request requiere `UserRoleAssignment` query por `(UserId, TenantId)`.

**Mitigation**:
- Los roles se cachean en el JWT claim por la duración del token (TTL típico 15min).
- En local, se inyectan vía header — no hay query.
- Si es bottleneck futuro, agregar caché en memoria por usuario+tenant (P2).

---

## 4. Brecha de implementación: Claims en LocalDev

**Hecho**: `LocalDevAuthenticationHandler` hoy NO agrega claims de rol.

**Decisión P1-RBAC**: En esta iteración se extiende para:
1. Leer header `X-Evidata-Dev-Roles` (ej. `"TenantOwner,ProcessOwner"`).
2. Agregar claims `"role"` al `ClaimsIdentity` construido en el handler.
3. `LocalDevCurrentUserContext` parsea los claims del request y expone `RoleNames`.

---

## 5. Mapeo a contrato público y permisos

### Contrato de `ResourcePermissionsViewModel` (doc 04 sección 1)

El ViewModel retornado por endpoints debe incluir:

```json
{
  "roleCodes": ["TenantOwner", "ProcessOwner"],
  "availableActions": [
    "ApproveProcessingActivity",
    "ValidateEvidence"
  ],
  "readOnly": false,
  "blockedActions": [
    "ActivateProcessingActivity",
    "AcceptGapWithRisk"
  ]
}
```

**Lógica**:
- `roleCodes`: nombres de los roles del usuario actual para este tenant.
- `availableActions`: permisos que el usuario **sí puede ejecutar** (intersección de roles + reglas custom).
- `blockedActions`: permisos que se **bloquean explícitamente** (e.g., ProcessOwner no puede aprobar su propia PA).
- `readOnly`: si verdadero, el recurso es de solo lectura (e.g., PA aprobada).

---

## 6. Plan de migración e integración

### 6.1 Seeding de roles mínimos

En migración EF (`SecurityDbContext`):

```csharp
// Up
modelBuilder.Entity<Role>().HasData(
    Role.Create("TenantOwner", "Propietario del tenant — acceso total", isSystemRole: true) { Id = Guid("00000001-...") },
    Role.Create("ComplianceAdmin", "Admin de cumplimiento", isSystemRole: true) { Id = Guid("00000002-...") },
    Role.Create("ProcessOwner", "Propietario de tratamiento", isSystemRole: true) { Id = Guid("00000003-...") },
    Role.Create("LegalReviewer", "Revisor legal", isSystemRole: true) { Id = Guid("00000004-...") },
    Role.Create("SecurityReviewer", "Revisor de seguridad", isSystemRole: true) { Id = Guid("00000005-...") },
    Role.Create("Auditor", "Auditor", isSystemRole: true) { Id = Guid("00000006-...") },
    Role.Create("Viewer", "Visor de solo lectura", isSystemRole: true) { Id = Guid("00000007-...") }
);
```

### 6.2 Seeding de permisos base

Se crean los permisos correspondi correspondientes a los 6 códigos del doc 04:

```csharp
modelBuilder.Entity<Permission>().HasData(
    Permission.Create("ProcessingActivity", "Approve", "Aprobar tratamiento"),
    Permission.Create("ProcessingActivity", "Activate", "Activar tratamiento"),
    Permission.Create("Evidence", "Validate", "Validar evidencia"),
    Permission.Create("Gap", "AcceptWithRisk", "Aceptar brecha con riesgo"),
    Permission.Create("Export", "GenerateOfficial", "Generar exportación oficial"),
    Permission.Create("Evidence", "Download", "Descargar evidencia")
);
```

### 6.3 Asignación de roles a permisos (seed mínimo)

```csharp
// En la migración, después de criar roles y permisos
var approveRole = roleRepository.GetByNameAsync("ProcessOwner");
var approvePermission = permissionRepository.GetByNameAsync("ProcessingActivity:Approve");
approveRole.AddPermission(approvePermission);  // ProcessOwner puede aprobar

var activateRole = roleRepository.GetByNameAsync("TenantOwner");
var activatePermission = permissionRepository.GetByNameAsync("ProcessingActivity:Activate");
activateRole.AddPermission(activatePermission);  // TenantOwner puede activar

// ... etc.
```

### 6.4 Asignación de roles a usuarios test

Para poder testear RBAC localmente, se crean usuarios de test con roles:

**En `LocalDevSeedUsers.cs`** (o nueva clase `LocalDevSeedRoles.cs`):

```csharp
// Crear usuario test
var testUser = UserProfile.Create(
    externalId: "dev-user-1",
    provider: "LocalDev",
    email: "processowner@tenant.local",
    displayName: "Process Owner Test",
    tenantId: DevTenantId
);
userProfileRepository.Add(testUser);

// Asignar rol
var processOwnerRole = roleRepository.GetByNameAsync("ProcessOwner");
var assignment = UserRoleAssignment.Create(testUser.Id, processOwnerRole.Id, DevTenantId);
userRoleAssignmentRepository.Add(assignment);
```

### 6.5 Integración en JwtCurrentUserContext y LocalDevCurrentUserContext

**JwtCurrentUserContext**:
```csharp
public class JwtCurrentUserContext : ICurrentUserContext
{
    public IReadOnlyList<string> RoleNames { get; private set; } = [];
    
    public JwtCurrentUserContext(IHttpContextAccessor httpContextAccessor)
    {
        // ... existing code ...
        
        // Nuevo: parsear claims de rol
        var roleClaims = principal.FindAll("role");  // puede ser múltiple claim o CSV
        RoleNames = roleClaims
            .Select(c => c.Value)
            .Where(v => !string.IsNullOrEmpty(v))
            .ToList()
            .AsReadOnly();
    }
}
```

**LocalDevCurrentUserContext**:
```csharp
public class LocalDevCurrentUserContext : ICurrentUserContext
{
    public const string RolesHeader = "X-Evidata-Dev-Roles";  // CSV: "TenantOwner,ProcessOwner"
    public IReadOnlyList<string> RoleNames { get; private set; } = [];
    
    public LocalDevCurrentUserContext(IHttpContextAccessor httpContextAccessor, ...)
    {
        // ... existing code ...
        
        // Nuevo: parsear header de roles
        var rolesRaw = context?.Request.Headers[RolesHeader].FirstOrDefault() ?? "";
        RoleNames = rolesRaw
            .Split(',')
            .Select(r => r.Trim())
            .Where(r => !string.IsNullOrEmpty(r))
            .ToList()
            .AsReadOnly();
    }
}
```

### 6.6 Integración en LocalDevAuthenticationHandler

**Objetivo**: Agregar claims de rol al `ClaimsIdentity` construido para desarrollo.

```csharp
public class LocalDevAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // ... código existente que lee headers ...
        
        var claims = new List<Claim>
        {
            new Claim("userId", userId.ToString()),
            new Claim("tenantId", tenantId.ToString()),
            new Claim("email", email),
            // Nuevo: agregar roles
            new Claim("role", rolesFromHeader)  // "TenantOwner,ProcessOwner"
        };
        
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        
        return AuthenticateResult.Success(ticket);
    }
}
```

---

## 7. Brechas y decisiones abiertas

### 7.1 Brecha: Sin endpoint de gestión de roles en P1

**Hecho**: No existe endpoint para `POST /api/v1/users/{userId}/roles` (asignar rol).

**Decisión P1-RBAC**: Se asume que roles se asignan vía:
- Seed en migraciones (roles iniciales a usuarios test).
- Admin manual en BD (en dev/test).
- P2: Endpoint administrativo `POST /admin/users/{userId}/roles`.

---

### 7.2 Brecha: Sin integración de PermissionCalculator en endpoints

**Hecho**: El `IAuthorizationEvaluator` existe pero no es usado por los endpoints de negocio (ProcessingInventory, etc.).

**Decisión P1-RBAC**: Se formaliza el enfoque conceptual en este ADR, pero la **integración de PolicyHandlers o atributos AuthorizeAttribute** queda como tarea de **P1-ProcessingInventory** o **P1-GapManagement**.

---

### 7.3 Brecha: Sin revoke inmediato (token expiration)

**Hecho**: Si se revoca un rol, el token JWT existente sigue siendo válido hasta expiración (TTL típico 15min).

**Decisión P1-RBAC**: Se acepta como riesgo aceptable para MVP. P2 puede agregar:
- Token invalidation list (blacklist).
- Roles check en cada request (cost: query por request).
- Short-lived tokens (TTL 5min vs. 15min).

---

### 7.4 Decisión abierta: Estructura de claim "role"

**Opciones**:
1. Claim múltiple: `claim name="role" value="TenantOwner"` + `claim name="role" value="ProcessOwner"` (estándar JWT).
2. Claim único CSV: `claim name="role" value="TenantOwner,ProcessOwner"`.
3. Claim único JSON: `claim name="role" value="["TenantOwner","ProcessOwner"]"` (raro, no recomendado).

**Decisión tentativa**: Usar **formato 1 (múltiples claims)** — es estándar JWT y `FindAll("role")` en .NET lo soporta naturalmente.

---

## 8. Resumen de decisiones

| Aspecto | Decisión | Justificación |
|---|---|---|
| Modelo de datos | Entidad `Role` (no enum) | Extensibilidad, auditoría, permisos dinámicos |
| Asignación | Tabla `UserRoleAssignment(UserId, RoleId, TenantId)` | Multi-tenancy, múltiples roles por usuario |
| Exposición de roles | Extender `ICurrentUserContext.RoleNames` | Separación de concerns, tipos fuertes |
| JWT | Agregar claim `"role"` (múltiple o CSV) | Standard JWT, compatible con validadores |
| LocalDev | Header `X-Evidata-Dev-Roles` en handlers | Testing sin BD, parallel a `userId/tenantId` |
| Seeding | 7 roles mínimos en migración EF | Baseline funcional, reproducible |
| Permisos | Tabla `PermissionRule` declarativa (P1) | Escalable, auditable, sin hardcoding |
| Gestión de roles | Sem endpoint POST en P1; P2 | MVP: seed + manual; futuro: API admin |
| Revoke | Sin invalidación inmediata; aceptar TTL | MVP trade-off; P2: blacklist si necesario |

---

## 9. Artefactos entregables

### Código

1. **Migración Security**: `20260709_RBAC_MinimalModel.cs`
   - Seed roles, permisos, asignaciones iniciales.

2. **Extensión ICurrentUserContext**: 
   - Interfaz actualizada con `IReadOnlyList<string> RoleNames`.
   - Implementaciones en `JwtCurrentUserContext`, `LocalDevCurrentUserContext`, `NullCurrentUserContext`.

3. **LocalDevAuthenticationHandler**:
   - Agregar lectura y claim de rol desde header `X-Evidata-Dev-Roles`.

4. **RoleConstants.cs**:
   ```csharp
   public static class RoleConstants
   {
       public const string TenantOwner = "TenantOwner";
       public const string ComplianceAdmin = "ComplianceAdmin";
       public const string ProcessOwner = "ProcessOwner";
       public const string LegalReviewer = "LegalReviewer";
       public const string SecurityReviewer = "SecurityReviewer";
       public const string Auditor = "Auditor";
       public const string Viewer = "Viewer";
   }
   ```

5. **LocalDevSeedRoles.cs** (opcional para P1):
   - Usuarios test con roles asignados (test_processowner@tenant.local, etc.).

### Documentación

1. Este ADR (11-adr-rbac-minimal-model.md).
2. Update `10-registro-de-decisiones-arquitectonicas.md` con mención a ADR-011.
3. Update `13-matriz-rbac-politicas-autorizacion.md` con mapeo de rol → permiso (si existe).

---

## 10. Referencias cruzadas

- **doc 04 (spec RBAC)**: `04-rbac-audit-evidence-gaps-contract.md` sección 1.
- **Arquitectura modular**: `02-arquitectura-general-backend.md`, ADR-001.
- **Multi-tenancy**: `10-registro-de-decisiones-arquitectonicas.md`, ADR-004.
- **Seguridad local**: `26-seguridad-desarrollo-local-sin-entra.md`, ADR-008.
- **Implementación Security module**: `src/Modules/Security/`.
- **Implementación Identity module**: `src/Modules/Identity/`.

---

**Versión**: 1.0  
**Fecha**: 2026-07-09  
**Autor**: Gandalf (Squad Architect)  
**Rama**: `dev/2026/07/09/adr-rbac-model`
