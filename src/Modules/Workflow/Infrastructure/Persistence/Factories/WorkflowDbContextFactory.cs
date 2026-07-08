using Evidata.Modules.Workflow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Evidata.Modules.Workflow.Infrastructure.Persistence.Factories;

public sealed class WorkflowDbContextFactory : IDesignTimeDbContextFactory<WorkflowDbContext>
{
    public WorkflowDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<WorkflowDbContext>()
            .UseNpgsql("Host=localhost;Database=evidata;Username=postgres;Password=postgres",
                b => b.MigrationsAssembly(typeof(WorkflowDbContextFactory).Assembly.FullName))
            .Options;

        return new WorkflowDbContext(options);
    }
}
