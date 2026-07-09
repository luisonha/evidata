using Microsoft.AspNetCore.Authorization;
using System.Reflection;

namespace Evidata.Tests.Unit.Identity;

/// <summary>
/// Verifica que los controllers críticos de seguridad tengan el atributo [Authorize]
/// para evitar que endpoints queden expuestos sin protección.
/// 
/// Esto captura el hallazgo crítico de que UserProfileController y RolesController
/// estaban completamente desprotegidos (no tenían [Authorize]).
/// </summary>
public class AuthorizationAttributeTests
{
    private static readonly Type[] CriticalControllers =
    {
        // Controladores de identidad y seguridad — DEBEN tener [Authorize]
        Type.GetType("Evidata.Modules.Identity.Api.UserProfileController, Evidata.Modules.Identity"),
        Type.GetType("Evidata.Modules.Security.Api.RolesController, Evidata.Modules.Security"),
        Type.GetType("Evidata.Modules.TenantManagement.Api.TenantsController, Evidata.Modules.TenantManagement"),
        Type.GetType("Evidata.Modules.Documents.Api.DocumentsController, Evidata.Modules.Documents"),
        Type.GetType("Evidata.Modules.Search.Api.SearchController, Evidata.Modules.Search"),
        Type.GetType("Evidata.Modules.Mcp.Api.McpController, Evidata.Modules.Mcp"),
        // Controllers que ya tenían [Authorize] (validación de consistencia)
        Type.GetType("Evidata.Modules.ProcessingInventory.Api.ProcessingActivitiesController, Evidata.Modules.ProcessingInventory"),
        Type.GetType("Evidata.Modules.Reporting.Api.ReportsController, Evidata.Modules.Reporting"),
        Type.GetType("Evidata.Modules.Evidence.Api.EvidenceController, Evidata.Modules.Evidence"),
        Type.GetType("Evidata.Modules.Audit.Api.AuditController, Evidata.Modules.Audit"),
        Type.GetType("Evidata.Modules.Workflow.Api.WorkflowController, Evidata.Modules.Workflow"),
        Type.GetType("Evidata.Modules.GapManagement.Api.GapsController, Evidata.Modules.GapManagement"),
    };

    /// <summary>
    /// Verifica que todos los controllers críticos tengan [Authorize] a nivel de clase.
    /// Esto es esencial para que no queden endpoints públicos por error de omisión.
    /// </summary>
    [Fact]
    public void AllCriticalControllers_MustHaveAuthorizeAttribute()
    {
        var controllersWithoutAuthorize = new List<string>();

        foreach (var controllerType in CriticalControllers.Where(t => t != null))
        {
            var authorizeAttr = controllerType.GetCustomAttribute<AuthorizeAttribute>();
            
            if (authorizeAttr is null)
            {
                controllersWithoutAuthorize.Add(controllerType.FullName);
            }
        }

        Assert.Empty(controllersWithoutAuthorize);
    }

    /// <summary>
    /// Verifica específicamente que UserProfileController tiene [Authorize].
    /// Este era el endpoint más crítico: permitía GET/POST/DELETE sin token.
    /// </summary>
    [Fact]
    public void UserProfileController_MustHaveAuthorizeAttribute()
    {
        var controllerType = Type.GetType("Evidata.Modules.Identity.Api.UserProfileController, Evidata.Modules.Identity");
        var authorizeAttr = controllerType.GetCustomAttribute<AuthorizeAttribute>();
        
        Assert.NotNull(authorizeAttr);
    }

    /// <summary>
    /// Verifica específicamente que RolesController tiene [Authorize].
    /// Este era el segundo endpoint más crítico: permitía GET/POST/DELETE de asignaciones sin token.
    /// </summary>
    [Fact]
    public void RolesController_MustHaveAuthorizeAttribute()
    {
        var controllerType = Type.GetType("Evidata.Modules.Security.Api.RolesController, Evidata.Modules.Security");
        var authorizeAttr = controllerType.GetCustomAttribute<AuthorizeAttribute>();
        
        Assert.NotNull(authorizeAttr);
    }

    /// <summary>
    /// Verifica que TenantsController tiene [Authorize].
    /// Esto evita que cualquiera pueda crear tenants o cambiar su estado.
    /// </summary>
    [Fact]
    public void TenantsController_MustHaveAuthorizeAttribute()
    {
        var controllerType = Type.GetType("Evidata.Modules.TenantManagement.Api.TenantsController, Evidata.Modules.TenantManagement");
        var authorizeAttr = controllerType.GetCustomAttribute<AuthorizeAttribute>();
        
        Assert.NotNull(authorizeAttr);
    }

    /// <summary>
    /// Verifica que DocumentsController tiene [Authorize].
    /// Documents contiene información sensible y NO debe ser público.
    /// </summary>
    [Fact]
    public void DocumentsController_MustHaveAuthorizeAttribute()
    {
        var controllerType = Type.GetType("Evidata.Modules.Documents.Api.DocumentsController, Evidata.Modules.Documents");
        var authorizeAttr = controllerType.GetCustomAttribute<AuthorizeAttribute>();
        
        Assert.NotNull(authorizeAttr);
    }

    /// <summary>
    /// Verifica que SearchController tiene [Authorize].
    /// Búsquedas se contextualizan por usuario/tenant y NO deben ser públicas.
    /// </summary>
    [Fact]
    public void SearchController_MustHaveAuthorizeAttribute()
    {
        var controllerType = Type.GetType("Evidata.Modules.Search.Api.SearchController, Evidata.Modules.Search");
        var authorizeAttr = controllerType.GetCustomAttribute<AuthorizeAttribute>();
        
        Assert.NotNull(authorizeAttr);
    }

    /// <summary>
    /// Verifica que McpController tiene [Authorize].
    /// MCP (compliance assistant) debe estar protegido a nivel de clase.
    /// </summary>
    [Fact]
    public void McpController_MustHaveAuthorizeAttribute()
    {
        var controllerType = Type.GetType("Evidata.Modules.Mcp.Api.McpController, Evidata.Modules.Mcp");
        var authorizeAttr = controllerType.GetCustomAttribute<AuthorizeAttribute>();
        
        Assert.NotNull(authorizeAttr);
    }
}
