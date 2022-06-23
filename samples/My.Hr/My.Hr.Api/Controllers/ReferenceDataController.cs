namespace My.Hr.Api.Controllers;

[Route("api/ref")]
[Produces(MediaTypeNames.Application.Json)]
public class ReferenceDataController : ControllerBase
{
    private readonly ReferenceDataContentWebApi _webApi;
    private readonly ReferenceDataOrchestrator _orchestrator;

    public ReferenceDataController(ReferenceDataContentWebApi webApi, ReferenceDataOrchestrator orchestrator)
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
    [ProducesResponseType(typeof(IEnumerable<USState>), (int)HttpStatusCode.OK)]
    public Task<IActionResult> USStateGetAll([FromQuery] IEnumerable<string>? codes = default, string? text = default) =>
        _webApi.GetAsync(Request, x => _orchestrator.GetWithFilterAsync<USState>(codes, text, x.RequestOptions.IncludeInactive));

    /// <summary> 
    /// Gets all of the <see cref="Gender"/> reference data items that match the specified criteria.
    /// </summary>
    /// <param name="codes">The reference data code list.</param>
    /// <param name="text">The reference data text (including wildcards).</param>
    /// <returns>A <see cref="GenderCollection"/>.</returns>
    [HttpGet("genders")]
    [ProducesResponseType(typeof(ReferenceDataMultiCollection), (int)HttpStatusCode.OK)]
    public Task<IActionResult> GenderGetAll([FromQuery] IEnumerable<string>? codes = default, string? text = default) =>
        _webApi.GetAsync(Request, x => _orchestrator.GetWithFilterAsync<Gender>(codes, text, x.RequestOptions.IncludeInactive));

    [HttpGet()]
    [ProducesResponseType(typeof(ReferenceDataMultiCollection), (int)HttpStatusCode.OK)]
    public Task<IActionResult> GetNamed() => _webApi.GetAsync(Request, p => _orchestrator.GetNamedAsync(p.RequestOptions));
}