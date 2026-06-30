namespace Contoso.Orders.Api.Controllers;

using OrderContract = Contoso.Orders.Contracts.Order;

[ApiController, Route("/api/orders"), OpenApiTag("Orders")]
public class OrderReadController(WebApi webApi, IOrderReadService service) : ControllerBase
{
    private readonly WebApi _webApi = webApi.ThrowIfNull();
    private readonly IOrderReadService _service = service.ThrowIfNull();

    [HttpGet("{id}"), HttpHead("{id}")]
    [ProducesResponseType(typeof(OrderContract), StatusCodes.Status200OK)]
    [ProducesNotFoundProblem()]
    public Task<IActionResult> GetAsync(string id, CancellationToken cancellationToken = default) => _webApi.GetAsync(Request, (_, ct)
        => _service.GetAsync(id.Required(), ct), cancellationToken: cancellationToken);

    [HttpGet]
    [ProducesResponseType(typeof(OrderLite[]), StatusCodes.Status200OK)]
    [Query(supportsOrderBy: true), Paging(supportsCount: true)]
    public Task<IActionResult> QueryAsync(CancellationToken cancellationToken = default) => _webApi.GetAsync(Request, (ro, ct)
        => _service.QueryAsync(ro.QueryArgs, ro.PagingArgs, ct), cancellationToken: cancellationToken);
}