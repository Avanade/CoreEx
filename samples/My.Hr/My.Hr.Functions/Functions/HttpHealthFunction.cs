using CoreEx.Healthchecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using System.Net;
using System.Net.Mime;
using System.Threading.Tasks;

namespace My.Hr.Functions;
public class HttpHealthFunction
{
    private readonly HealthService _health;

    public HttpHealthFunction(HealthService health)
    {
        _health = health;
    }

    [FunctionName("HealthInfo")]
    [OpenApiOperation(operationId: "Run", tags: new[] { "health" })]
    [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: MediaTypeNames.Application.Json, bodyType: typeof(HealthReportEntry), Description = "The OK response")]
    public async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Function, "get", Route = "health")] HttpRequest req)
        => await _health.RunAsync().ConfigureAwait(false);
}