namespace Contoso.Products.Api.Controllers;

[ApiController, Route("/api/products"), OpenApiTag("Products")]
public class ProductReadController(WebApi webApi, IProductReadService service) : ControllerBase
{
    private readonly WebApi _webApi = webApi.ThrowIfNull();
    private readonly IProductReadService _service = service.ThrowIfNull();

    [HttpGet("{id}"), HttpHead("{id}")]
    [ProducesResponseType(typeof(Product), 200)]
    [ProducesNotFoundProblem()]
    public Task<IActionResult> GetAsync(string id) => _webApi.GetAsync(Request, (_, _) => _service.GetAsync(id.Required()));

    [HttpGet]
    [ProducesResponseType(typeof(ProductLite[]), 200)]
    [Query(supportsOrderBy: true), Paging(supportsCount: true)]
    public Task<IActionResult> QueryAsync() => _webApi.GetAsync(Request, (ro, _) => _service.QueryAsync(ro.QueryArgs, ro.PagingArgs));

    [HttpGet("$query")]
    [ProducesResponseType(typeof(JsonElement), 200)]
    public Task<IActionResult> QuerySchemaAsync() => _webApi.GetAsync(Request, (ro, _) => _service.QuerySchemaAsync());
}