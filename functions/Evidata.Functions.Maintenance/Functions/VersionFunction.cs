using System.Net;
using System.Text.Json;
using Evidata.ServiceDefaults;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Evidata.Functions.Maintenance.Functions;

public class VersionFunction
{
    [Function("GetVersion")]
    public async Task<HttpResponseData> GetVersion(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "version")] HttpRequestData req,
        FunctionContext executionContext)
    {
        var versionInfo = VersionHelper.GetVersionInfo();

        var httpResponse = executionContext.GetHttpResponseData()!;
        httpResponse.StatusCode = HttpStatusCode.OK;
        httpResponse.Headers.Add("Content-Type", "application/json; charset=utf-8");

        var responseBody = versionInfo.ToResponse();
        await httpResponse.WriteAsJsonAsync(responseBody);

        return httpResponse;
    }
}
