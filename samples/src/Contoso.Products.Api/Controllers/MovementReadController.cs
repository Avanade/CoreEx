namespace Contoso.Products.Api.Controllers;

[ApiController, Route("/api/inventory/movements"), OpenApiTag("Inventory")]
public class MovementReadController(WebApi webApi, IMovementReadService service) : ControllerBase
{
    private readonly WebApi _webApi = webApi.ThrowIfNull();
    private readonly IMovementReadService _service = service.ThrowIfNull();

    [HttpGet()]
    [ProducesResponseType<Movement[]>(200)]
    [Query, Paging(supportsCount: true)]
    public Task<IActionResult> QueryAsync(CancellationToken cancellationToken = default) => _webApi.GetAsync(Request, (ro, ct)
        => _service.QueryAsync(ro.QueryArgs, ro.PagingArgs, ct), HttpStatusCode.OK, cancellationToken: cancellationToken);

    [HttpGet("$query")]
    [ProducesResponseType(typeof(JsonElement), 200)]
    public Task<IActionResult> QuerySchemaAsync(CancellationToken cancellationToken = default) => _webApi.GetAsync(Request, (ro, ct) => _service.QuerySchemaAsync(ct), cancellationToken: cancellationToken);
}