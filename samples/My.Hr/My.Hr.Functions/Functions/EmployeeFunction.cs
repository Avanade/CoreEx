using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mime;
using System.Threading.Tasks;
using CoreEx.FluentValidation;
using CoreEx.WebApis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;
using My.Hr.Business.Models;
using My.Hr.Business.Services;
using My.Hr.Business.Validators;

namespace My.Hr.Functions.Functions;

public class EmployeeFunction
{
    private readonly WebApi _webApi;
    private readonly EmployeeService _service;

    public EmployeeFunction(WebApi webApi, EmployeeService service)
    {
        _webApi = webApi;
        _service = service;
    }

    [FunctionName("Get")]
    [OpenApiOperation(operationId: "Get", tags: new[] { "employee" })]
    [OpenApiParameter(name: "id", Description = "The employee id", Required = true, In = ParameterLocation.Path, Type = typeof(Guid))]
    [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: MediaTypeNames.Application.Json, bodyType: typeof(Employee), Description = "Employee record")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Description = "Not found")]
    public Task<IActionResult> GetAsync([HttpTrigger(AuthorizationLevel.Function, "get", Route = "api/employees/{id}")] HttpRequest request, Guid id)
    {
        return _webApi.GetAsync(request, _ => _service.GetEmployeeAsync(id));
    }

    [FunctionName("GetAll")]
    [OpenApiOperation(operationId: "GetAll", tags: new[] { "employee" })]
    [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: MediaTypeNames.Application.Json, bodyType: typeof(List<Employee>), Description = "Employee records")]
    public Task<IActionResult> GetAllAsync([HttpTrigger(AuthorizationLevel.Function, "get", Route = "api/employees")] HttpRequest request)
    => _webApi.GetAsync(request, p => _service.GetAllAsync(p.RequestOptions.Paging));

    // [FunctionName("Create")]
    // [OpenApiOperation(operationId: "Create", tags: new[] { "employee" })]
    // [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
    // [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Created, Description = "Created employee record")]
    public Task<IActionResult> CreateAsync([HttpTrigger(AuthorizationLevel.Function, "post", Route = "api/employees")] HttpRequest request)
        => _webApi.PostAsync<Employee, Employee>(request, p => _service.AddEmployeeAsync(p.Value!),
           statusCode: HttpStatusCode.Created, validator: new EmployeeValidator().Wrap(), locationUri: e => new Uri($"api/employees/{e.Id}", UriKind.RelativeOrAbsolute));

    [FunctionName("Update")]
    // [OpenApiOperation(operationId: "Update", tags: new[] { "employee" })]
    // [OpenApiParameter(name: "id", Description = "The employee id", Required = true, In = ParameterLocation.Path, Type = typeof(Guid))]
    // [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
    // [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: MediaTypeNames.Application.Json, bodyType: typeof(Employee), Description = "Employee record")]
    // [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Description = "Not found")]
    public Task<IActionResult> UpdateAsync([HttpTrigger(AuthorizationLevel.Function, "put", Route = "api/employees/{id}")] HttpRequest request, Guid id)
        => _webApi.PutAsync<Employee, Employee>(request, p => _service.UpdateEmployeeAsync(p.Value!, id), validator: new EmployeeValidator().Wrap());

    [FunctionName("Patch")]
    public Task<IActionResult> PatchAsync([HttpTrigger(AuthorizationLevel.Function, "patch", Route = "api/employees/{id}")] HttpRequest request, Guid id)
        => _webApi.PatchAsync(request, get: _ => _service.GetEmployeeAsync(id), put: p => _service.UpdateEmployeeAsync(p.Value!, id), validator: new EmployeeValidator().Wrap());

    [FunctionName("Delete")]
    public Task<IActionResult> DeleteAsync([HttpTrigger(AuthorizationLevel.Function, "delete", Route = "api/employees/{id}")] HttpRequest request, Guid id)
        => _webApi.DeleteAsync(request, _ => _service.DeleteEmployeeAsync(id));
}
