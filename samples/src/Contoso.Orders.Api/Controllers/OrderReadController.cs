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
    public Task<IActionResult> GetAsync(string id) => _webApi.GetAsync(Request, (_, _)
        => _service.GetAsync(id.Required()));

    [HttpGet]
    [ProducesResponseType(typeof(OrderLite[]), StatusCodes.Status200OK)]
    [Query(supportsOrderBy: true), Paging(supportsCount: true)]
    public Task<IActionResult> QueryAsync() => _webApi.GetAsync(Request, (ro, _)
        => _service.QueryAsync(ro.QueryArgs, ro.PagingArgs));
}