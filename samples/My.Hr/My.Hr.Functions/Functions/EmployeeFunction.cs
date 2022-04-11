using CoreEx.FluentValidation;
using CoreEx.WebApis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using My.Hr.Business.Models;
using My.Hr.Business.Services;
using My.Hr.Business.Validators;
using System;
using System.Net;
using System.Threading.Tasks;

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
    public Task<IActionResult> GetAsync([HttpTrigger(AuthorizationLevel.Function, "get", Route = "api/employees/{id}")] HttpRequest request, Guid id)
        => _webApi.GetAsync(request, _ => _service.GetEmployeeAsync(id));

    [FunctionName("GetAll")]
    public Task<IActionResult> GetAllAsync([HttpTrigger(AuthorizationLevel.Function, "get", Route = "api/employees")] HttpRequest request)
    => _webApi.GetAsync(request, p => _service.GetAllAsync(p.RequestOptions.Paging));

    [FunctionName("Create")]
    public Task<IActionResult> CreateAsync([HttpTrigger(AuthorizationLevel.Function, "post", Route = "api/employees")] HttpRequest request)
        => _webApi.PostAsync<Employee, Employee>(request, p => _service.AddEmployeeAsync(p.Validate<Employee, EmployeeValidator>()),
           statusCode: HttpStatusCode.Created, locationUri: e => new Uri($"api/employees/{e.Id}", UriKind.RelativeOrAbsolute));

    [FunctionName("Update")]
    public Task<IActionResult> UpdateAsync([HttpTrigger(AuthorizationLevel.Function, "put", Route = "api/employees/{id}")] HttpRequest request, Guid id)
        => _webApi.PutAsync<Employee, Employee>(request, p => _service.UpdateEmployeeAsync(p.Validate<Employee, EmployeeValidator>(), id));

    [FunctionName("Delete")]
    public Task<IActionResult> DeleteAsync([HttpTrigger(AuthorizationLevel.Function, "delete", Route = "api/employees/{id}")] HttpRequest request, Guid id)
        => _webApi.DeleteAsync(request, _ => _service.DeleteEmployeeAsync(id));
}
