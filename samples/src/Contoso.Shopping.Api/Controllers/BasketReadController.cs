namespace Contoso.Shopping.Api.Controllers;

[ApiController, Route("/api/baskets"), OpenApiTag("Baskets")]
public class BasketReadController(WebApi webApi, IBasketReadService service) : ControllerBase
{
    private readonly WebApi _webApi = webApi.ThrowIfNull();
    private readonly IBasketReadService _service = service.ThrowIfNull();

    [HttpGet("{basketId}"), HttpHead("{basketId}")]
    [ProducesResponseType(typeof(Basket), 200)]
    [ProducesNotFoundProblem()]
    public Task<IActionResult> GetAsync(string basketId, CancellationToken cancellationToken = default) => _webApi.GetWithResultAsync(Request, (_, ct) => _service.GetAsync(basketId.Required(), ct), cancellationToken: cancellationToken);

    [HttpGet("/api/customers/{customerId}/baskets")]
    [ProducesResponseType<Basket[]>(200)]
    [Query, Paging(supportsCount: true)]
    public Task<IActionResult> QueryAsync(string customerId, CancellationToken cancellationToken = default) => _webApi.GetAsync(Request, (ro, ct)
        => _service.QueryAsync(customerId.Required(), ro.QueryArgs, ro.PagingArgs, ct), HttpStatusCode.OK, cancellationToken: cancellationToken);

    [HttpGet("/api/customers/{customerId}/baskets/$query")]
    [ProducesResponseType(typeof(JsonElement), 200)]
    public Task<IActionResult> QuerySchemaAsync(string customerId, CancellationToken cancellationToken = default) => _webApi.GetAsync(Request, (ro, ct) => _service.QuerySchemaAsync(ct), cancellationToken: cancellationToken);
}