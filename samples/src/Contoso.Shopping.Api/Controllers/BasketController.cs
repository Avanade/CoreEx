namespace Contoso.Shopping.Api.Controllers;

[ApiController, Route("/api/baskets"), OpenApiTag("Baskets")]
public class BasketController(WebApi webApi, IBasketService service) : ControllerBase
{
    private readonly WebApi _webApi = webApi.ThrowIfNull();
    private readonly IBasketService _service = service.ThrowIfNull();

    [OpenApiTag("Customers")]
    [HttpPost("/api/customers/{customerId}/baskets")]
    [IdempotencyKey]
    [ProducesResponseType(typeof(Basket), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<IActionResult> CreateAsync(string customerId) => _webApi.PostWithResultAsync<Basket>(Request, async (ro, _) =>
    {
        ro.WithLocationUri(p => new Uri($"/api/baskets/{p.Id}", UriKind.Relative));
        return await _service.CreateAsync(customerId.Required()).ConfigureAwait(false);
    });

    [HttpPut("{basketId}/apply-discount/{coupon}")]
    [ProducesResponseType(typeof(Basket), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<IActionResult> ApplyDiscountAsync(string basketId, string coupon) => _webApi.PutWithResultAsync<Basket>(Request, (_, _)
        => _service.ApplyDiscountAsync(basketId.Required(), coupon.Required()));

    [HttpPost("{basketId}/checkout")]
    [ProducesResponseType(typeof(Basket), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<IActionResult> CheckoutAsync(string basketId) => _webApi.PostWithResultAsync<Basket>(Request, (_, _)
        => _service.CheckoutAsync(basketId.Required()), HttpStatusCode.OK);

    [HttpPost("{basketId}/items")]
    [IdempotencyKey]
    [Accepts<BasketItemAddRequest>]
    [ProducesResponseType(typeof(Basket), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<IActionResult> ItemAddAsync(string basketId) => _webApi.PostWithResultAsync<BasketItemAddRequest, Basket>(Request, (ro, _)
        => _service.ItemAddAsync(basketId.Required(), ro.Value), HttpStatusCode.OK);

    [HttpPut("{basketId}/items/{basketItemId}")]
    [Accepts<BasketItemUpdateRequest>]
    [ProducesResponseType(typeof(Basket), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<IActionResult> ItemUpdateAsync(string basketId, string basketItemId) => _webApi.PutWithResultAsync<BasketItemUpdateRequest, Basket>(Request, (ro, _)
        => _service.ItemUpdateAsync(basketId.Required(), basketItemId.Required(), ro.Value), HttpStatusCode.OK);

    [HttpDelete("{basketId}/items/{basketItemId}")]
    [ProducesResponseType(typeof(Basket), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<IActionResult> ItemDeleteAsync(string basketId, string basketItemId) => _webApi.DeleteWithResultAsync<Basket>(Request, (_, _)
        => _service.ItemDeleteAsync(basketId.Required(), basketItemId.Required()));
}