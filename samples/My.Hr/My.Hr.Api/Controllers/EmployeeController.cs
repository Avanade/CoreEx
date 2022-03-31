using System.Net;
using System.Net.Mime;
using CoreEx.FluentValidation;
using CoreEx.WebApis;
using Microsoft.AspNetCore.Mvc;
using My.Hr.Business.Models;
using My.Hr.Business.Services;
using My.Hr.Business.Validators;

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
    [HttpGet("{id}", Name = nameof(GetAsync))]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(Employee), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public Task<IActionResult> GetAsync(Guid id)
        => _webApi.GetAsync(Request, _ => _service.GetEmployeeAsync(id));

    /// <summary>
    /// Creates a new <see cref="Employee"/>.
    /// </summary>
    /// <returns>The created <see cref="Employee"/>.</returns>
    [HttpPost("")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(Employee), (int)HttpStatusCode.Created)]
    public Task<IActionResult> Create()
        => _webApi.PostAsync<Employee, Employee>(Request, r => _service.AddEmployeeAsync(r.Validate<Employee, EmployeeValidator>()));

    /// <summary>
    /// Updates an existing <see cref="Employee"/>.
    /// </summary>
    /// <param name="id">The <see cref="Employee"/> identifier.</param>
    /// <returns>The updated <see cref="Employee"/>.</returns>
    [HttpPut("{id}")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(Employee), (int)HttpStatusCode.OK)]
    public Task<IActionResult> Update(Guid id)
        => _webApi.PutAsync<Employee, Employee>(Request, r => _service.UpdateEmployeeAsync(r.Value!, id));

    /// <summary>
    /// Deletes the specified <see cref="Employee"/>.
    /// </summary>
    /// <param name="id">The Id.</param>
    [HttpDelete("{id}")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    public Task<IActionResult> Delete(Guid id)
        => _webApi.DeleteAsync(Request, _ => _service.DeleteEmployeeAsync(id));

    /// <summary>
    /// Gets all <see cref="Employee"/>.
    /// </summary>
    /// <returns>All <see cref="Employee"/>.</returns>
    [HttpGet("", Name = nameof(GetAll))]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(IEnumerable<Employee>), (int)HttpStatusCode.OK)]
    public Task<IActionResult> GetAll()
        => _webApi.GetAsync(Request, _ => _service.GetAllAsync());


    /// <summary>
    /// Performs <see cref="Employee"/> verification in an asynchronous process.
    /// </summary>
    [HttpPost("{id}/verify")]
    [ProducesResponseType((int)HttpStatusCode.Accepted)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public Task<IActionResult> Verify(Guid id)
           => _webApi.PostAsync(Request, apiParam => _service.VerifyEmployeeAsync(id), HttpStatusCode.Accepted);

}