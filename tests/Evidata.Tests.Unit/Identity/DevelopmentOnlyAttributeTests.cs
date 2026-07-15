using Evidata.Modules.Identity.Api;
using Evidata.Modules.Identity.Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Evidata.Tests.Unit.Identity;

/// <summary>
/// Verification tests for DevelopmentOnly attribute.
/// 
/// CRITICAL SECURITY TEST:
/// Verifies that the attribute exists and is correctly applied to dev controllers.
/// Full integration tests verify 404 responses in non-dev environments.
/// </summary>
public class DevelopmentOnlyAttributeTests
{
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
