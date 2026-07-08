using Evidata.Modules.Identity.Infrastructure.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Evidata.Tests.Unit.LocalAuth;

public class LocalDevCurrentUserContextTests
{
    private static LocalDevCurrentUserContext BuildContext(string? userId = null, string? tenantId = null, string? email = null)
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();

        if (userId is not null) httpContext.Request.Headers[LocalDevCurrentUserContext.UserIdHeader] = userId;
        if (tenantId is not null) httpContext.Request.Headers[LocalDevCurrentUserContext.TenantIdHeader] = tenantId;
        if (email is not null) httpContext.Request.Headers[LocalDevCurrentUserContext.EmailHeader] = email;

        accessor.HttpContext.Returns(httpContext);
        return new LocalDevCurrentUserContext(accessor, NullLogger<LocalDevCurrentUserContext>.Instance);
    }

    [Fact]
    public void WithValidHeaders_IsAuthenticated()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var ctx = BuildContext(userId.ToString(), tenantId.ToString(), "dev@test.com");

        Assert.True(ctx.IsAuthenticated);
        Assert.Equal(userId, ctx.UserId);
        Assert.Equal(tenantId, ctx.TenantId);
        Assert.Equal("dev@test.com", ctx.Email);
    }

    [Fact]
    public void WithNoHeaders_IsNotAuthenticated()
    {
        var ctx = BuildContext();

        Assert.False(ctx.IsAuthenticated);
        Assert.Equal(Guid.Empty, ctx.UserId);
        Assert.Equal(Guid.Empty, ctx.TenantId);
    }

    [Fact]
    public void WithInvalidGuidHeaders_IsNotAuthenticated()
    {
        var ctx = BuildContext("not-a-guid", "also-not-a-guid");

        Assert.False(ctx.IsAuthenticated);
    }

    [Fact]
    public void WithNullHttpContext_IsNotAuthenticated()
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns((HttpContext?)null);

        var ctx = new LocalDevCurrentUserContext(accessor, NullLogger<LocalDevCurrentUserContext>.Instance);

        Assert.False(ctx.IsAuthenticated);
    }
}
