# Investigación: `ResponsibleAreaId` y entidad área responsable

**Fecha:** 2026-07-12  
**Contexto:** Resolución de Decisión #7 (decisiones-ambiguedades.md)  
**Investigador:** Aragorn  
**Hallazgo Principal:** NO existe entidad "Area" o "ResponsibleArea" definida en el proyecto. El campo `ResponsibleAreaId` es un GUID sin validación de existencia.

---

## Resumen Ejecutivo

La investigación del campo `responsibleAreaId` mencionado en Sprint 3 (validación de invitaciones y filtros de usuarios) reveló que:

1. **No existe una entidad Area/ResponsibleArea real** en ningún módulo del proyecto.
2. **ResponsibleAreaId existe como Guid? en Invitation entity** (Identity/Domain) pero SIN validación de referencia a una entidad real.
3. **ProcessingActivity.Department es un string**, no un GUID — no es un target válido para referencia cruzada.
4. **El campo `AreaId` en ProcessingActivityDetailViewModel (Fase 5)** no corresponde a una entidad existente; mapeado desde el string `Department`.

**Conclusión:** La validación "debe pertenecer al tenant" NO PUEDE implementarse actualmente porque no existe la entidad a validar.

---

## Búsquedas Realizadas

### 1. Búsqueda de Entidad Area

**Comando:**
```bash
find /src/Modules -name "*Area*.cs" -o -name "*Department*.cs" -o -name "*Responsible*.cs"
grep -r "class.*Area\|class.*Department\|class.*Responsible" /src/Modules --include="*.cs"
```

**Resultado:** ❌ Ningún archivo encontrado. No existe clase, record, o entidad llamada `Area`, `Department` o `ResponsibleArea`.

### 2. Búsqueda de ResponsibleAreaId en C#

**Comando:**
```bash
grep -r "responsibleAreaId\|ResponsibleAreaId" /src/Modules --include="*.cs"
```

**Resultado:** ✓ Encontrado en 1 ubicación:

- **Archivo:** `/src/Modules/Identity/Domain/Invitation.cs`  
- **Línea:** 32  
- **Código:**
  ```csharp
  /// <summary>
  /// The responsible area/department for this invitation (optional).
  /// </summary>
  public Guid? ResponsibleAreaId { get; private set; }
  ```

**Análisis:** El campo existe pero es un GUID sin validación, sin relación con una tabla de "Areas", sin guard de tenant.

### 3. Inspección de ProcessingActivity (Candidato ProcessingInventory)

**Archivo:** `/src/Modules/ProcessingInventory/Domain/ProcessingActivity.cs`

**Campos relevantes:**
```csharp
// Línea 48-49: Responsable del tratamiento (string, no GUID)
public string? Controller { get; private set; }

// Línea 51-52: Departamento (string, no referencia de entidad)
public string? Department { get; private set; }
```

**Análisis:** 
- `Department` es un string literal, **NO una referencia a una entidad Area**.
- No implementa guardias de tenant para esta propiedad.
- Este campo es de **lectura/escritura sin validación de existencia**.

### 4. ViewModels en ProcessingInventory (Fase 5)

**Archivo:** `/src/Modules/ProcessingInventory/Application/ViewModels/ProcessingActivityControlViewModels.cs`

**Línea 35:**
```csharp
public sealed record ProcessingActivityDetailViewModel(
    Guid Id,
    Guid TenantId,
    string Name,
    string? Description,
    Guid? AreaId,        // ← Campo presente pero sin entidad backing
    Guid? OwnerUserId,
    ...
)
```

**Análisis:**
- El ViewModel expone `AreaId` como Guid? (Fase 5).
- **Origen desconocido:** No hay mapeo explícito de ProcessingActivity → AreaId en los DTOs revisados.
- Probablemente es un placeholder preparado para una entidad Area que NO existe aún.

### 5. Cross-Module Integration Pattern

**Búsqueda:**
```bash
grep -r "using Evidata.Modules" /src/Modules/Identity --include="*.cs"
grep -r "ServiceCollection.*Add" /src/Modules/Identity/IdentityModule.cs
```

**Resultado:**
- Identity module **no referencia otros módulos directamente** en Application/Domain.
- Se comunica vía:
  - **Servicios registrados en DI** (AddScoped<T>)
  - **Interfaces abstractas** (ej: `IAuditService` inyectable)
  - **ITenantScoped marker interface** (Identity/Application/Abstractions)

**Patrón recomendado:** Para consumir una entidad de ProcessingInventory desde Identity:
1. Vía **Application Service (Query/Handler pattern)** expuesto por ProcessingInventory
2. **NO importar directamente Domain entity** — rompe aislamiento modular

---

## ITenantScoped Implementation

**Ubicación:** `/src/Modules/Identity/Application/Abstractions/ITenantScoped.cs`

```csharp
public interface ITenantScoped
{
    Guid TenantId { get; }
}
```

**Adopción:** Implementada en múltiples Domain entities:
- `Invitation` (Identity)
- `UserProfile` (Identity)
- `ProcessingActivity` (ProcessingInventory)
- `Evidence`, `EvidenceValidation` (Evidence module)
- `ComplianceGap` (GapManagement module)
- etc.

**Conclusión:** Si se crea una entidad `Area`, DEBE implementar `ITenantScoped` para garantizar aislamiento por tenant.

---

## Opciones de Resolución

Debido a que **no existe una entidad Area real**, se presentan 3 opciones:

### Opción 1: Crear entidad Area mínima (RECOMENDADO para Fase 0+)

**Ubicación:** `Evidata.Modules.ProcessingInventory.Domain.Area`

```csharp
public class Area : ITenantScoped
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    // Factory, validations, etc.
}
```

**Ventajas:**
- Permite validación real: "¿Existe AreaId X en TenantId Y?"
- Aislamiento por tenant (`ITenantScoped`)
- Reutilizable en Invitation, UserProfile, ProcessingActivity

**Desventajas:**
- Requiere creación de tabla, migración EF Core
- Dependencia cruzada: Identity → ProcessingInventory

**Consumo desde Identity:**
```csharp
// Identity/Application/Commands/InviteUserCommandHandler.cs
public class InviteUserCommandHandler
{
    private readonly ProcessingInventoryDbContext _piDb;
    
    public async Task<Guid> HandleAsync(InviteUserCommand cmd)
    {
        // Validar que AreaId exista en tenant
        if (cmd.ResponsibleAreaId.HasValue)
        {
            var area = await _piDb.Areas
                .Where(a => a.Id == cmd.ResponsibleAreaId && a.TenantId == cmd.TenantId)
                .FirstOrDefaultAsync();
                
            if (area == null)
                throw new InvalidResponsibleAreaException(...);
        }
        
        var invitation = Invitation.Create(..., cmd.ResponsibleAreaId);
        // ...
    }
}
```

---

### Opción 2: Almacenar ResponsibleAreaId como string (SIMPLE, pero sin validación)

**Cambio en Identity.Invitation:**
```csharp
/// <summary>
/// Departamento/área responsable (string literal, sin validación de existencia)
/// </summary>
public string? ResponsibleArea { get; private set; }
```

**Ventajas:**
- Sin dependencias cross-module
- Sin migraciones adicionales
- Funcional para Fase 0

**Desventajas:**
- No valida existencia de área
- No valida tenant membership
- Duplica lógica con ProcessingActivity.Department
- Frágil (typos, inconsistencias)

---

### Opción 3: Dejar como GUID, sin validación (BLOQUEADO para Fase 0)

**Estado actual:**
```csharp
public Guid? ResponsibleAreaId { get; private set; } // Sin validación
```

**Ventajas:**
- Código ya existe

**Desventajas:**
- Acepta cualquier GUID sin validar
- No cumple requisito: "debe pertenecer al tenant"
- Bloqueador documentado en Decisión #7

---

## Recomendación

**Para Fase 0 (Sprint 3):**
1. **Usar Opción 2 (string)** como solución transitoria
   - Cambiar `Guid? ResponsibleAreaId` → `string? ResponsibleArea` en Invitation
   - Mapear responsablemente en GET /users response (ej: `"responsibleArea": "area_123"` devuelve el string)
   - Documentar: "Validación de área no implementada aún; solo almacenamiento"

2. **Crear tarea de Fase 1-2:** "Crear entidad Area en ProcessingInventory + validación cruzada desde Identity"

**Para Fase 1+:**
1. Implementar Opción 1 (entidad Area)
2. Migración: normalizar datos de `Invitation.ResponsibleArea` (string) → `Invitation.ResponsibleAreaId` (GUID con FK)
3. Agregar validación de tenant en guardias

---

## Referencias Cruzadas

| Componente | Ubicación | Observación |
|---|---|---|
| `ITenantScoped` | `Identity/Application/Abstractions/ITenantScoped.cs` | Marker interface para aislamiento |
| `Invitation.ResponsibleAreaId` | `Identity/Domain/Invitation.cs:32` | Guid? sin validación actual |
| `ProcessingActivity.Department` | `ProcessingInventory/Domain/ProcessingActivity.cs:52` | String, no GUID |
| `ProcessingActivityDetailViewModel.AreaId` | `ProcessingInventory/Application/ViewModels/...:35` | Placeholder Fase 5 |
| Decisión #7 | `13-decisiones-ambiguedades.md:230` | Fuente de esta investigación |

---

## Próximos Pasos

1. **Decisión del equipo:** ¿Opción 2 (string) para Fase 0 o retrasar Sprint 3 para Opción 1 (Area entity)?
2. **Si Opción 2:** Actualizar documentos 05-contratos, 04-endpoints para reflejar type `string` en responsibleArea
3. **Si Opción 1:** Crear tarea formal de creación de Area entity en ProcessingInventory con migración EF Core
4. **Bloqueo:** No iniciar implementación de `GET /users?responsibleAreaId=...` filtro hasta resolver esta ambigüedad

---

**Estado:** ✅ Investigación completada | 🟡 Bloqueador de decisión | ⏳ Esperando decisión de equipo
