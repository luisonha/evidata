using Evidata.Modules.Identity.Application.Abstractions;
using Evidata.Modules.Identity.Infrastructure.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Evidata.Tests.Unit.TenantGuard;

public class TenantIsolationMiddlewareTests
{
    private readonly ICurrentUserContext _userContext = Substitute.For<ICurrentUserContext>();

    private TenantIsolationMiddleware CreateMiddleware(RequestDelegate next)
        => new(next, NullLogger<TenantIsolationMiddleware>.Instance);

    [Fact]
    public async Task Invoke_UnauthenticatedUser_PassesThrough()
    {
        // Arrange
        _userContext.IsAuthenticated.Returns(false);
        var called = false;
        var middleware = CreateMiddleware(_ => { called = true; return Task.CompletedTask; });
        var context = new DefaultHttpContext();

        // Act
        await middleware.InvokeAsync(context, _userContext);

        // Assert
        Assert.True(called);
    }

    [Fact]
    public async Task Invoke_SameTenant_PassesThrough()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        _userContext.IsAuthenticated.Returns(true);
        _userContext.TenantId.Returns(tenantId);
        var called = false;
        var middleware = CreateMiddleware(_ => { called = true; return Task.CompletedTask; });
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Tenant-Id"] = tenantId.ToString();

        // Act
        await middleware.InvokeAsync(context, _userContext);

        // Assert
        Assert.True(called);
    }

    [Fact]
    public async Task Invoke_DifferentTenant_Returns403()
    {
        // Arrange
        _userContext.IsAuthenticated.Returns(true);
        _userContext.TenantId.Returns(Guid.NewGuid());
        var middleware = CreateMiddleware(_ => Task.CompletedTask);
        var context = new DefaultHttpContext();
        context.Response.Body = new System.IO.MemoryStream();
        context.Request.Headers["X-Tenant-Id"] = Guid.NewGuid().ToString();

        // Act
        await middleware.InvokeAsync(context, _userContext);

        // Assert
        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
    }

    private static IServiceProvider BuildServiceProvider(ICurrentUserContext ctx)
    {
        var services = new ServiceCollection();
        services.AddSingleton(ctx);
        return services.BuildServiceProvider();
    }
}
