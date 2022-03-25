using System.Net;
using System.Net.Mime;
using CoreEx.FluentValidation;
using CoreEx.WebApis;
using Microsoft.AspNetCore.Mvc;
using My.Hr.Business.Models;
using My.Hr.Business.Services;

namespace My.Hr.Api.Controllers;

public class ReferenceDataController : ControllerBase
{
    private readonly WebApi _webApi;
    private readonly ReferenceDataService _service;

    public ReferenceDataController(WebApi webApi, ReferenceDataService service)
    {
        _webApi = webApi;
        _service = service;
    }

    /// <summary> 
    /// Gets all of the <see cref="USState"/> reference data items that match the specified criteria.
    /// </summary>
    /// <param name="codes">The reference data code list.</param>
    /// <param name="text">The reference data text (including wildcards).</param>
    /// <returns>A RefDataNamespace.USState collection.</returns>
    [HttpGet()]
    [Route("ref/usStates")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(IEnumerable<USState>), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    public Task<IActionResult> USStateGetAll(List<string>? codes = default, string? text = default) =>
        _webApi.GetAsync<IEnumerable<USState>>(Request, _ => _service.GetAll(codes, text));

}
