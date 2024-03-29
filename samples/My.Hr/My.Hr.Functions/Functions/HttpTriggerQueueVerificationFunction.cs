using System.Net;
using System.Net.Mime;
using System.Threading.Tasks;
using CoreEx.FluentValidation;
using CoreEx.AspNetCore.WebApis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;
using My.Hr.Business;
using My.Hr.Business.External.Contracts;
using My.Hr.Business.Validators;

namespace My.Hr.Functions;

public class HttpTriggerQueueVerificationFunction
{
    private readonly WebApiPublisher _webApiPublisher;
    private readonly HrSettings _settings;

    public HttpTriggerQueueVerificationFunction(WebApiPublisher webApiPublisher, HrSettings settings)
    {
        _webApiPublisher = webApiPublisher;
        _settings = settings;
    }

    [FunctionName(nameof(HttpTriggerQueueVerificationFunction))]
    [OpenApiOperation(operationId: "Run", tags: new[] { "employee" })]
    [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
    [OpenApiRequestBody(MediaTypeNames.Application.Json, typeof(EmployeeVerificationRequest), Description = "The **EmployeeVerification** payload")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Accepted, contentType: MediaTypeNames.Text.Plain, bodyType: typeof(string), Description = "The OK response")]
    public Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Function, "post", Route = "employee/verify")] HttpRequest request)
        => _webApiPublisher.PublishAsync(request, new WebApiPublisherArgs<EmployeeVerificationRequest>(_settings.VerificationQueueName) { Validator = new EmployeeVerificationValidator().Wrap() });
}