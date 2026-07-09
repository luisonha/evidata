using Evidata.Modules.TenantManagement.Application.Commands;
using Evidata.Modules.TenantManagement.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Evidata.Tests.Unit.TenantManagement;

public class CreateTenantCommandHandlerTests
{
    [Fact]
    public async Task Handle_NewSlug_CreatesTenant()
    {
        var repo = Substitute.For<ITenantRepository>();
        repo.GetBySlugAsync("acme", default).Returns((Tenant?)null);
        var handler = new CreateTenantCommandHandler(repo, NullLogger<CreateTenantCommandHandler>.Instance);

        var result = await handler.HandleAsync(new CreateTenantCommand("acme", "Acme Corp"));

        Assert.Equal("acme", result.Slug);
        await repo.Received(1).AddAsync(Arg.Any<Tenant>(), default);
    }

    [Fact]
    public async Task Handle_DuplicateSlug_ReturnsExistingTenant()
    {
        var existing = Tenant.Create("acme", "Existing");
        var repo = Substitute.For<ITenantRepository>();
        repo.GetBySlugAsync("acme", default).Returns(existing);
        var handler = new CreateTenantCommandHandler(repo, NullLogger<CreateTenantCommandHandler>.Instance);

        var result = await handler.HandleAsync(new CreateTenantCommand("acme", "Acme Corp"));

        Assert.Equal("acme", result.Slug);
        await repo.DidNotReceive().AddAsync(Arg.Any<Tenant>(), default);
    }
}
