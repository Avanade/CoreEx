namespace Contoso.Products.Api.Controllers;

[ApiController, Route("/api/products"), OpenApiTag("Products")]
public class InventoryController(WebApi webApi, IInventoryService service) : ControllerBase
{
    private readonly WebApi _webApi = webApi.ThrowIfNull();
    private readonly IInventoryService _service = service.ThrowIfNull();

    [HttpGet("{id}/on-hand")]
    [ProducesResponseType(typeof(Product), 200)]
    [ProducesNotFoundProblem()]
    public Task<IActionResult> GetOnHandAsync(string id) => _webApi.GetAsync(Request, (_, _) => _service.GetOnHandAsync(id.Required()));
}