using Evidata.Modules.Identity.Api;
using Evidata.Modules.Identity.Infrastructure.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Xunit;

namespace Evidata.Tests.Unit.Identity;

/// <summary>
/// Verification tests for DevelopmentOnly attribute.
///
/// CRITICAL SECURITY TEST:
/// Verifies both that the attribute is applied to dev controllers AND that its
/// runtime behavior actually blocks requests in non-Development environments.
/// The reflection-only checks below are NOT sufficient by themselves — an attribute
/// can be present and still fail to execute correctly (wrong filter interface, DI
/// resolution failure, etc.). <see cref="OnActionExecutionAsync_NonDevelopment_ReturnsNotFound"/>
/// and <see cref="OnActionExecutionAsync_Development_CallsNext"/> exercise the real
/// filter logic end-to-end against mocked ActionExecutingContext/IHostEnvironment,
/// which is what actually determines the HTTP response in production.
/// </summary>
public class DevelopmentOnlyAttributeTests
{
    private static ActionExecutingContext BuildContext(string environmentName)
    {
        var hostEnvironment = Substitute.For<IHostEnvironment>();
        hostEnvironment.EnvironmentName.Returns(environmentName);

        var services = new ServiceCollection();
        services.AddSingleton(hostEnvironment);
        var httpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };

        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ControllerActionDescriptor());

        return new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            controller: new object());
    }

    /// <summary>
    /// CRITICAL: reproduces the exact vulnerability found in code review — in a
    /// non-Development environment (e.g. "Production"), the filter MUST short-circuit
    /// the pipeline with a 404, never calling the actual controller action.
    /// </summary>
    [Fact]
    public async Task OnActionExecutionAsync_NonDevelopment_ReturnsNotFound()
    {
        var attribute = new DevelopmentOnlyAttribute();
        var context = BuildContext("Production");
        var nextCalled = false;

        await attribute.OnActionExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(context, context.Filters, context.Controller));
        });

        Assert.False(nextCalled, "El filtro no debe invocar la acción real en un entorno no-Development.");
        Assert.IsType<NotFoundResult>(context.Result);
    }

    /// <summary>
    /// Regression check: the guard must NOT block legitimate Development traffic.
    /// </summary>
    [Fact]
    public async Task OnActionExecutionAsync_Development_CallsNext()
    {
        var attribute = new DevelopmentOnlyAttribute();
        var context = BuildContext("Development");
        var nextCalled = false;

        await attribute.OnActionExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(context, context.Filters, context.Controller));
        });

        Assert.True(nextCalled, "El filtro debe permitir el paso en Development.");
        Assert.Null(context.Result);
    }

    /// <summary>
    /// Test: DevelopmentOnlyAttribute exists and can be instantiated
    /// </summary>
    [Fact]
    public void DevelopmentOnlyAttribute_CanBeInstantiated()
    {
        // Act
        var attribute = new DevelopmentOnlyAttribute();

        // Assert
        Assert.NotNull(attribute);
    }

    /// <summary>
    /// Test: DevAuthController has DevelopmentOnly attribute applied at class level
    /// </summary>
    [Fact]
    public void DevAuthController_HasDevelopmentOnlyAttribute()
    {
        // Arrange
        var controllerType = typeof(DevAuthController);

        // Act
        var attributes = controllerType.GetCustomAttributes(typeof(DevelopmentOnlyAttribute), false);

        // Assert
        Assert.NotEmpty(attributes);
        Assert.IsType<DevelopmentOnlyAttribute>(attributes[0]);
    }

    /// <summary>
    /// Test: DevAdminController has DevelopmentOnly attribute applied at class level
    /// </summary>
    [Fact]
    public void DevAdminController_HasDevelopmentOnlyAttribute()
    {
        // Arrange
        var controllerType = typeof(DevAdminController);

        // Act
        var attributes = controllerType.GetCustomAttributes(typeof(DevelopmentOnlyAttribute), false);

        // Assert
        Assert.NotEmpty(attributes);
        Assert.IsType<DevelopmentOnlyAttribute>(attributes[0]);
    }
}
