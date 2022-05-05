using CoreEx.RefData;
using CoreEx.WebApis;
using Microsoft.AspNetCore.Mvc;
using My.Hr.Business.Models;
using System.Net;
using System.Net.Mime;

namespace My.Hr.Api.Controllers;

[Route("api/ref")]
public class ReferenceDataController : ControllerBase
{
    private readonly WebApi _webApi;
    private readonly ReferenceDataOrchestrator _orchestrator;

    public ReferenceDataController(WebApi webApi, ReferenceDataOrchestrator orchestrator)
    {
        _webApi = webApi;
        _orchestrator = orchestrator;
    }

    /// <summary> 
    /// Gets all of the <see cref="USState"/> reference data items that match the specified criteria.
    /// </summary>
    /// <param name="codes">The reference data code list.</param>
    /// <param name="text">The reference data text (including wildcards).</param>
    /// <returns>A <see cref="USStateCollection"/>.</returns>
    [HttpGet("usstates")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(IEnumerable<USState>), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    public Task<IActionResult> USStateGetAll([FromQuery] IEnumerable<string>? codes = default, string? text = default) =>
        _webApi.GetAsync(Request, x => _orchestrator.GetWithFilterAsync<USState>(codes, text, x.RequestOptions.IncludeInactive));

    /// <summary> 
    /// Gets all of the <see cref="Gender"/> reference data items that match the specified criteria.
    /// </summary>
    /// <param name="codes">The reference data code list.</param>
    /// <param name="text">The reference data text (including wildcards).</param>
    /// <returns>A <see cref="GenderCollection"/>.</returns>
    [HttpGet("genders")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(IEnumerable<Gender>), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    public Task<IActionResult> GenderGetAll([FromQuery] IEnumerable<string>? codes = default, string? text = default) =>
        _webApi.GetAsync(Request, x => _orchestrator.GetWithFilterAsync<Gender>(codes, text, x.RequestOptions.IncludeInactive));
}