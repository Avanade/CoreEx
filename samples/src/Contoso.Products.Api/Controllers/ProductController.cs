namespace Contoso.Products.Api.Controllers;

[ApiController, Route("/api/products"), OpenApiTag("Products")]
public class ProductController(WebApi webApi, IProductService service) : ControllerBase
{
    private readonly WebApi _webApi = webApi.ThrowIfNull();
    private readonly IProductService _service = service.ThrowIfNull();

    [HttpPost]
    [Accepts<Product>]
    [ProducesResponseType<Product>(201)]
    [IdempotencyKey]
    public Task<IActionResult> PostAsync(CancellationToken cancellationToken = default) => _webApi.PostAsync<Product, Product>(Request, (ro, ct) =>
    {
        ro.WithLocationUri(p => new Uri($"/api/products/{p.Id}", UriKind.Relative));
        return _service.CreateAsync(ro.Value, ct);
    }, cancellationToken: cancellationToken);

    [HttpPut("{id}")]
    [Accepts<Product>]
    [ProducesResponseType(typeof(Product), 200)]
    [ProducesNotFoundProblem()]
    public Task<IActionResult> PutAsync(string id, CancellationToken cancellationToken = default) => _webApi.PutAsync<Product, Product>(Request, (ro, ct)
        => _service.UpdateAsync(ro.Value.Adjust(p => p.Id = id.Required()), ct), cancellationToken: cancellationToken);

    [HttpPatch("{id}")]
    [Accepts<Product>(HttpNames.MergePatchJsonMediaTypeName)]
    [ProducesResponseType(typeof(Product), 200)]
    [ProducesNotFoundProblem()]
    public Task<IActionResult> PatchAsync(string id, CancellationToken cancellationToken = default) => _webApi.PatchAsync<Product>(Request,
        get: (ro, ct) => _service.GetAsync(id.Required(), ct),
        put: (ro, ct) => _service.UpdateAsync(ro.Value.Adjust(p => p.Id = id), ct),
        cancellationToken: cancellationToken);

    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    public Task<IActionResult> DeleteAsync(string id, CancellationToken cancellationToken = default) => _webApi.DeleteAsync(Request, (_, ct)
        => _service.DeleteAsync(id.Required(), ct), cancellationToken: cancellationToken);
}