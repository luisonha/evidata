using System.Net;
using System.Text.Json;
using Evidata.ServiceDefaults;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Evidata.Functions.SearchIndexing.Functions;

public class VersionFunction
{
    [Function("GetVersion")]
    public async Task<HttpResponseData> GetVersion(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "version")] HttpRequestData req)
    {
        var versionInfo = VersionHelper.GetVersionInfo();

        var httpResponse = req.CreateResponse(HttpStatusCode.OK);

        var responseBody = versionInfo.ToResponse();
        await httpResponse.WriteAsJsonAsync(responseBody);

        return httpResponse;
    }
}
