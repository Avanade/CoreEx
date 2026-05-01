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
    public Task<IActionResult> PostAsync() => _webApi.PostAsync<Product, Product>(Request, (ro, _) =>
    {
        ro.WithLocationUri(p => new Uri($"/api/products/{p.Id}", UriKind.Relative));
        return _service.CreateAsync(ro.Value);
    });

    [HttpPut("{id}")]
    [Accepts<Product>]
    [ProducesResponseType(typeof(Product), 200)]
    [ProducesNotFoundProblem()]
    public Task<IActionResult> PutAsync(string id) => _webApi.PutAsync<Product, Product>(Request, (ro, _)
        => _service.UpdateAsync(ro.Value.Adjust(p => p.Id = id.Required())));

    [HttpPatch("{id}")]
    [Accepts<Product>(HttpNames.MergePatchJsonMediaTypeName)]
    [ProducesResponseType(typeof(Product), 200)]
    [ProducesNotFoundProblem()]
    public Task<IActionResult> PatchAsync(string id) => _webApi.PatchAsync<Product>(Request,
        get: (ro, _) => _service.GetAsync(id.Required()),
        put: (ro, _) => _service.UpdateAsync(ro.Value.Adjust(p => p.Id = id)));

    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    public Task<IActionResult> DeleteAsync(string id) => _webApi.DeleteAsync(Request, (_, _)
        => _service.DeleteAsync(id.Required()));
}