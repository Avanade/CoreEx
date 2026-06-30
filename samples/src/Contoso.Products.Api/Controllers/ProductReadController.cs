namespace Contoso.Products.Api.Controllers;

[ApiController, Route("/api/products"), OpenApiTag("Products")]
public class ProductReadController(WebApi webApi, IProductReadService service) : ControllerBase
{
    private readonly WebApi _webApi = webApi.ThrowIfNull();
    private readonly IProductReadService _service = service.ThrowIfNull();

    [HttpGet("{id}"), HttpHead("{id}")]
    [ProducesResponseType(typeof(Product), 200)]
    [ProducesNotFoundProblem()]
    public Task<IActionResult> GetAsync(string id, CancellationToken cancellationToken = default) => _webApi.GetAsync(Request, (_, ct) => _service.GetAsync(id.Required(), ct), cancellationToken: cancellationToken);

    [HttpGet]
    [ProducesResponseType(typeof(ProductLite[]), 200)]
    [Query(supportsOrderBy: true), Paging(supportsCount: true)]
    public Task<IActionResult> QueryAsync(CancellationToken cancellationToken = default) => _webApi.GetAsync(Request, (ro, ct) => _service.QueryAsync(ro.QueryArgs, ro.PagingArgs, ct), cancellationToken: cancellationToken);

    [HttpGet("$query")]
    [ProducesResponseType(typeof(JsonElement), 200)]
    public Task<IActionResult> QuerySchemaAsync(CancellationToken cancellationToken = default) => _webApi.GetAsync(Request, (ro, ct) => _service.QuerySchemaAsync(ct), cancellationToken: cancellationToken);
}