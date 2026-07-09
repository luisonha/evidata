using Evidata.Modules.LegalKnowledge.Application.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Evidata.Modules.LegalKnowledge.Api;

[ApiController]
[Route("api/legal-sources")]
public class LegalSourcesController(ListLegalSourcesQueryHandler handler) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LegalSourceDto>>> List(
        [FromQuery] bool? activeOnly = true, CancellationToken ct = default)
    {
        var result = await handler.HandleAsync(activeOnly, ct);
        return Ok(result);
    }
}

[ApiController]
[Route("api/legal-obligations")]
public class LegalObligationsController(ListLegalObligationsQueryHandler handler) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LegalObligationDto>>> List(
        [FromQuery] string? status = null, CancellationToken ct = default)
    {
        var result = await handler.HandleAsync(status, ct);
        return Ok(result);
    }
}
