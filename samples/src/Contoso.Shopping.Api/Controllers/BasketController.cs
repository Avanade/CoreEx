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
    public Task<IActionResult> CreateAsync(string customerId, CancellationToken cancellationToken = default) => _webApi.PostWithResultAsync<Basket>(Request, async (ro, ct) =>
    {
        ro.WithLocationUri(p => new Uri($"/api/baskets/{p.Id}", UriKind.Relative));
        return await _service.CreateAsync(customerId.Required(), ct).ConfigureAwait(false);
    }, cancellationToken: cancellationToken);

    [HttpPut("{basketId}/apply-discount/{coupon}")]
    [ProducesResponseType(typeof(Basket), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<IActionResult> ApplyDiscountAsync(string basketId, string coupon, CancellationToken cancellationToken = default) => _webApi.PutWithResultAsync<Basket>(Request, (_, ct)
        => _service.ApplyDiscountAsync(basketId.Required(), coupon.Required(), ct), cancellationToken: cancellationToken);

    [HttpPost("{basketId}/checkout")]
    [ProducesResponseType(typeof(Basket), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<IActionResult> CheckoutAsync(string basketId, CancellationToken cancellationToken = default) => _webApi.PostWithResultAsync<Basket>(Request, (_, ct)
        => _service.CheckoutAsync(basketId.Required(), ct), HttpStatusCode.OK, cancellationToken: cancellationToken);

    [HttpPost("{basketId}/items")]
    [IdempotencyKey]
    [Accepts<BasketItemAddRequest>]
    [ProducesResponseType(typeof(Basket), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<IActionResult> ItemAddAsync(string basketId, CancellationToken cancellationToken = default) => _webApi.PostWithResultAsync<BasketItemAddRequest, Basket>(Request, (ro, ct)
        => _service.ItemAddAsync(basketId.Required(), ro.Value, ct), HttpStatusCode.OK, cancellationToken: cancellationToken);

    [HttpPut("{basketId}/items/{basketItemId}")]
    [Accepts<BasketItemUpdateRequest>]
    [ProducesResponseType(typeof(Basket), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<IActionResult> ItemUpdateAsync(string basketId, string basketItemId, CancellationToken cancellationToken = default) => _webApi.PutWithResultAsync<BasketItemUpdateRequest, Basket>(Request, (ro, ct)
        => _service.ItemUpdateAsync(basketId.Required(), basketItemId.Required(), ro.Value, ct), HttpStatusCode.OK, cancellationToken: cancellationToken);

    [HttpDelete("{basketId}/items/{basketItemId}")]
    [ProducesResponseType(typeof(Basket), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<IActionResult> ItemDeleteAsync(string basketId, string basketItemId, CancellationToken cancellationToken = default) => _webApi.DeleteWithResultAsync<Basket>(Request, (_, ct)
        => _service.ItemDeleteAsync(basketId.Required(), basketItemId.Required(), ct), cancellationToken: cancellationToken);
}