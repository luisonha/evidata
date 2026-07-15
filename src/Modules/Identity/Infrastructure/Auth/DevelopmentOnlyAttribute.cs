using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Evidata.Modules.Identity.Infrastructure.Auth;

/// <summary>
/// Action filter that ensures the endpoint is only accessible in Development environment.
/// Returns 404 Not Found in non-Development environments to avoid revealing the endpoint exists.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class DevelopmentOnlyAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var hostEnvironment = context.HttpContext.RequestServices.GetRequiredService<IHostEnvironment>();

        if (!hostEnvironment.IsDevelopment())
        {
            // Return 404 Not Found to indicate the resource doesn't exist in non-dev environments
            context.Result = new NotFoundResult();
            return;
        }

        // Proceed to the action
        await next();
    }
}
