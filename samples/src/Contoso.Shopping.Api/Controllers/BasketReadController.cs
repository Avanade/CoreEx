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
}