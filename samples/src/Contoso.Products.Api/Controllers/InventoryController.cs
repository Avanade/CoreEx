namespace Contoso.Products.Api.Controllers;

[ApiController, Route("/api/products"), OpenApiTag("Products")]
public class InventoryController(WebApi webApi, IInventoryService service) : ControllerBase
{
    private readonly WebApi _webApi = webApi.ThrowIfNull();
    private readonly IInventoryService _service = service.ThrowIfNull();

    [HttpGet("{id}/on-hand")]
    [ProducesResponseType(typeof(decimal), 200)]
    [ProducesNotFoundProblem()]
    public Task<IActionResult> GetOnHandAsync(string id, CancellationToken cancellationToken = default) => _webApi.GetAsync(Request, (_, ct) => _service.GetOnHandAsync(id.Required(), ct), cancellationToken: cancellationToken);
}