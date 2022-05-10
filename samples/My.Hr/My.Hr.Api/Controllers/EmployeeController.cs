using CoreEx.Http;
using CoreEx.WebApis;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using My.Hr.Business.Models;
using My.Hr.Business.Services;
using My.Hr.Business.Validators;
using System.Net;
using System.Net.Mime;

namespace My.Hr.Api.Controllers;

[Route("api/employees")]
public class EmployeeController : ControllerBase
{
    private readonly WebApi _webApi;
    private readonly EmployeeService _service;

    public EmployeeController(WebApi webApi, EmployeeService service)
    {
        _webApi = webApi;
        _service = service;
    }

    /// <summary>
    /// Gets the specified <see cref="Employee"/>.
    /// </summary>
    /// <param name="id">The <see cref="Employee"/> identifier.</param>
    /// <returns>The selected <see cref="Employee"/> where found.</returns>
    [HttpGet("{id}", Name = "Get")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(Employee), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public Task<IActionResult> GetAsync(Guid id)
        => _webApi.GetAsync(Request, _ => _service.GetEmployeeAsync(id));

    /// <summary>
    /// Gets all <see cref="Employee"/>.
    /// </summary>
    /// <returns>All <see cref="Employee"/>.</returns>
    [HttpGet("", Name = "GetAll")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(IEnumerable<Employee>), (int)HttpStatusCode.OK)]
    public Task<IActionResult> GetAllAsync()
        => _webApi.GetAsync(Request, p => _service.GetAllAsync(p.RequestOptions.Paging));

    /// <summary>
    /// Creates a new <see cref="Employee"/>.
    /// </summary>
    /// <returns>The created <see cref="Employee"/>.</returns>
    [HttpPost("", Name = "Create")]
    [AcceptsBody(typeof(Employee))]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(Employee), (int)HttpStatusCode.Created)]
    public Task<IActionResult> CreateAsync()
        => _webApi.PostAsync(Request, p => _service.AddEmployeeAsync(p.Value!),
           statusCode: HttpStatusCode.Created, validator: new EmployeeValidator().Convert(), locationUri: e => new Uri($"api/employees/{e.Id}", UriKind.RelativeOrAbsolute));

    /// <summary>
    /// Updates an existing <see cref="Employee"/>.
    /// </summary>
    /// <param name="id">The <see cref="Employee"/> identifier.</param>
    /// <returns>The updated <see cref="Employee"/>.</returns>
    [HttpPut("{id}", Name = "Update")]
    [AcceptsBody(typeof(Employee))]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(Employee), (int)HttpStatusCode.OK)]
    public Task<IActionResult> UpdateAsync(Guid id)
        => _webApi.PutAsync(Request, p => _service.UpdateEmployeeAsync(p.Value!, id), validator: new EmployeeValidator().Convert());

    /// <summary>
    /// Patches an existing <see cref="Employee"/>.
    /// </summary>
    /// <param name="id">The <see cref="Employee"/> identifier.</param>
    /// <returns>The updated <see cref="Employee"/>.</returns>
    [HttpPatch("{id}", Name = "Patch")]
    [AcceptsBody(typeof(Employee), HttpConsts.MergePatchMediaTypeName)]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(Employee), (int)HttpStatusCode.OK)]
    public Task<IActionResult> PatchAsync(Guid id)
        => _webApi.PatchAsync(Request, get: _ => _service.GetEmployeeAsync(id), put: p => _service.UpdateEmployeeAsync(p.Value!, id), validator: new EmployeeValidator().Convert());

    /// <summary>
    /// Deletes the specified <see cref="Employee"/>.
    /// </summary>
    /// <param name="id">The Id.</param>
    [HttpDelete("{id}", Name = "Delete")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    public Task<IActionResult> DeleteAsync(Guid id)
        => _webApi.DeleteAsync(Request, _ => _service.DeleteEmployeeAsync(id));

    /// <summary>
    /// Performs <see cref="Employee"/> verification in an asynchronous process.
    /// </summary>
    [HttpPost("{id}/verify", Name = "Verify")]
    [ProducesResponseType((int)HttpStatusCode.Accepted)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public Task<IActionResult> VerifyAsync(Guid id)
        => _webApi.PostAsync(Request, apiParam => _service.VerifyEmployeeAsync(id), HttpStatusCode.Accepted);
}